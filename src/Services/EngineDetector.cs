using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using SourceGit.Models;

namespace SourceGit.Services;

/// <summary>
/// Multi-source engine detection following the same algorithm as Unreal Engine's
/// own project discovery (FProjectDescriptor::EngineAssociation resolution).
///
/// EngineAssociation values in .uproject:
///   - Empty string (""): Walk up directory tree from .uproject looking for Engine/Build/Build.version
///   - GUID ({...}): Look up in HKCU\Software\Epic Games\Unreal Engine\Builds (value name → path)
///     or Install.ini on macOS/Linux
///   - Version string ("5.4"): Launcher binary install, also check Install.ini/registry
///   - Path string: Use directly (relative to .uproject directory)
///
/// Cross-platform: Windows, macOS, Linux.
/// </summary>
public static class EngineDetector
{
    /// <summary>
    /// Detect the engine path for a UE project. Returns empty string if not found.
    /// Priority: user-local override > team config > env var > .uproject > relative probe > parent scan > known paths > registry/ini.
    /// </summary>
    public static string Detect(UProjectMeta meta, string repoPath)
    {
        // 1. User-local override (.unrealsync/local.json enginePathOverride)
        var localState = ConfigService.LoadLocalState(repoPath);
        if (!string.IsNullOrEmpty(localState.EnginePathOverride))
        {
            var overridePath = ResolveRelativePath(repoPath, localState.EnginePathOverride);
            if (IsValidEngineRoot(overridePath))
                return overridePath;
        }

        // 2. Team-shared config override (.unrealsync.json engine.path)
        var config = ConfigService.LoadConfig(repoPath);
        if (!string.IsNullOrEmpty(config.Engine?.Path))
        {
            var configPath = ResolveRelativePath(repoPath, config.Engine.Path);
            if (IsValidEngineRoot(configPath))
                return configPath;
        }

        // 3. Environment variable
        var envPaths = new[] { "UNREAL_ENGINE_PATH", "UE_ROOT" };
        foreach (var env in envPaths)
        {
            var envPath = Environment.GetEnvironmentVariable(env);
            if (!string.IsNullOrEmpty(envPath) && IsValidEngineRoot(envPath))
                return envPath;
        }

        // 4. UProject EngineAssociation
        var uprojectDir = GetUProjectDirectory(repoPath);
        var searchBase = uprojectDir.Length > 0 ? uprojectDir : repoPath;
        var association = meta.EngineAssociation?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(association))
        {
            // Empty EngineAssociation — walk up directory tree from .uproject location.
            // This is the RECOMMENDED setup for source builds: place the engine in a
            // parent directory and leave EngineAssociation empty.
            // Matches UE's own behavior: FProjectDescriptor searches upward for Engine/Build/Build.version.
            if (TryFindEngineByDirectoryWalk(searchBase, out var walkPath))
                return walkPath;
        }
        else if (association.StartsWith('{') && association.EndsWith('}'))
        {
            // GUID — look up in Install.ini (macOS/Linux) or registry (Windows).
            // The GUID is a registry VALUE NAME under ...\\Builds, not a subkey.
            if (TryInstallIniLookup(association, out var iniPath))
                return iniPath;
            if (TryRegistryLookup(association, out var registryPath))
                return registryPath;
            // Custom source build GUIDs may not be registered anywhere —
            // fall through to relative probes and parent scan below.
        }
        else if (System.Version.TryParse(association, out var version))
        {
            // Version string (e.g., "5.7") — try Install.ini/registry first, then probe source, then launcher.
            var versionKey = $"{version.Major}.{version.Minor}";

            // Install.ini / registry may map version strings to custom paths
            if (TryInstallIniLookup(versionKey, out var iniPath))
                return iniPath;
            if (TryRegistryLookup(versionKey, out var regPath))
                return regPath;

            // Source build probes (higher priority than launcher installs)
            var sourceProbes = new[]
            {
                $"../UnrealEngine",
                $"../UE{version.Major}",
                $"../UE{version.Major}.{version.Minor}",
                $"../../UnrealEngine",
                $"../../UE{version.Major}",
            };
            foreach (var probe in sourceProbes)
            {
                var probePath = ResolveRelativePath(searchBase, probe);
                if (IsValidEngineRoot(probePath))
                    return probePath;
            }

            // Launcher install paths (lower priority)
            foreach (var installPath in GetKnownInstallPaths(versionKey))
            {
                if (IsValidEngineRoot(installPath))
                    return installPath;
            }
        }
        else
        {
            // Relative/absolute path (e.g., "../UE5")
            var resolved = ResolveRelativePath(searchBase, association);
            if (IsValidEngineRoot(resolved))
                return resolved;
        }

        // 5. Relative probe (../UnrealEngine, ../UE5, ../UE4, ../Engine)
        var relativeProbes = new[] { "../UnrealEngine", "../UE5", "../UE4", "../Engine" };
        foreach (var probe in relativeProbes)
        {
            var probePath = ResolveRelativePath(repoPath, probe);
            if (IsValidEngineRoot(probePath))
                return probePath;
        }

        // 6. Scan parent directories for source-build markers
        var parentScanResult = ScanParentDirectoriesForSourceBuild(repoPath);
        if (!string.IsNullOrEmpty(parentScanResult))
            return parentScanResult;

        // 7. Known install paths (launcher installs — last resort)
        foreach (var knownPath in GetKnownInstallPaths(string.Empty))
        {
            if (IsValidEngineRoot(knownPath))
                return knownPath;
        }

        // 8. Broad registry / Install.ini scan
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (TryRegistryLookup(string.Empty, out var broadRegPath))
                return broadRegPath;
        }
        else
        {
            if (TryInstallIniLookup(string.Empty, out var broadIniPath))
                return broadIniPath;
        }

        return string.Empty;
    }

    /// <summary>
    /// Walk up the directory tree from the .uproject location looking for a source-build engine.
    /// A source build is identified by the presence of Engine/Build/Build.version.
    /// This is the canonical detection method when EngineAssociation is empty.
    /// </summary>
    private static bool TryFindEngineByDirectoryWalk(string startPath, out string enginePath)
    {
        enginePath = string.Empty;
        var current = new DirectoryInfo(startPath);

        // Walk up to 10 levels — covers typical project layouts where engine is a parent/sibling
        for (var i = 0; i < 10 && current != null; i++)
        {
            var buildVersionFile = Path.Combine(current.FullName, "Engine", "Build", "Build.version");
            if (File.Exists(buildVersionFile))
            {
                if (IsValidEngineRoot(current.FullName))
                {
                    enginePath = current.FullName;
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Look up an engine path from Install.ini on macOS or Linux.
    /// The INI file lives at:
    ///   macOS: ~/Library/Application Support/Epic/UnrealEngine/Install.ini
    ///   Linux: ~/.config/Epic/UnrealEngine/Install.ini
    /// Format:
    ///   [Installations]
    ///   {GUID}=/path/to/engine
    ///   5.4=/path/to/UE_5.4
    /// </summary>
    private static bool TryInstallIniLookup(string key, out string path)
    {
        path = string.Empty;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        string iniPath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            iniPath = Path.Combine(home, "Library", "Application Support", "Epic", "UnrealEngine", "Install.ini");
        }
        else // Linux
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            iniPath = Path.Combine(home, ".config", "Epic", "UnrealEngine", "Install.ini");
        }

        if (!File.Exists(iniPath))
            return false;

        try
        {
            var inInstallationsSection = false;
            foreach (var line in File.ReadLines(iniPath))
            {
                var trimmed = line.Trim();

                // Skip empty lines and comments
                if (trimmed.Length == 0 || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                    continue;

                // Section header
                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    inInstallationsSection = trimmed.Equals("[Installations]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inInstallationsSection)
                    continue;

                // Key=Value pair
                var eqIndex = trimmed.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

                var iniKey = trimmed.Substring(0, eqIndex).Trim();
                var iniValue = trimmed.Substring(eqIndex + 1).Trim();

                // Broad scan (empty key) or specific key match
                if (string.IsNullOrEmpty(key))
                {
                    if (!string.IsNullOrEmpty(iniValue) && IsValidEngineRoot(iniValue))
                    {
                        path = iniValue;
                        return true;
                    }
                }
                else if (iniKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(iniValue) && IsValidEngineRoot(iniValue))
                    {
                        path = iniValue;
                        return true;
                    }
                }
            }
        }
        catch
        {
            // INI read/parse failure — best effort
        }

        return false;
    }

    /// <summary>
    /// Scan parent/grandparent directories looking for UE source build markers.
    /// Checks for Setup scripts, .git dir, and GenerateProjectFiles as evidence of a source build.
    /// </summary>
    private static string ScanParentDirectoriesForSourceBuild(string repoPath)
    {
        var current = new DirectoryInfo(repoPath);
        for (var i = 0; i < 4 && current?.Parent != null; i++)
        {
            current = current.Parent;

            // If this directory IS a valid engine root, return it
            if (IsValidEngineRoot(current.FullName))
                return current.FullName;

            // Otherwise scan its immediate subdirectories for engine markers
            try
            {
                foreach (var subDir in current.GetDirectories())
                {
                    if (IsLikelySourceBuild(subDir.FullName) && IsValidEngineRoot(subDir.FullName))
                        return subDir.FullName;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Heuristic: does this directory look like a UE source build (not launcher install)?
    /// </summary>
    private static bool IsLikelySourceBuild(string path)
    {
        if (!Directory.Exists(path))
            return false;

        var hasSetup = File.Exists(Path.Combine(path, "Setup.bat"))
            || File.Exists(Path.Combine(path, "Setup.sh"));
        var hasGenerateProjectFiles = File.Exists(Path.Combine(path, "GenerateProjectFiles.bat"))
            || File.Exists(Path.Combine(path, "GenerateProjectFiles.sh"));
        var hasGit = Directory.Exists(Path.Combine(path, ".git"));

        return hasSetup || hasGenerateProjectFiles || hasGit;
    }

    /// <summary>
    /// Validate a candidate engine directory by checking for the platform-specific editor binary.
    /// </summary>
    public static bool IsValidEngineRoot(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return false;

        if (File.Exists(GetEditorBinaryPath(path, isUe5: true)))
            return true;

        if (File.Exists(GetEditorBinaryPath(path, isUe5: false)))
            return true;

        return false;
    }

    /// <summary>
    /// Get the editor binary path for the current platform.
    /// </summary>
    public static string GetEditorBinaryPath(string enginePath, bool isUe5)
    {
        var editorName = isUe5 ? "UnrealEditor" : "UE4Editor";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(enginePath, "Engine", "Binaries", "Win64", $"{editorName}.exe");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Path.Combine(enginePath, "Engine", "Binaries", "Mac", $"{editorName}.app", "Contents", "MacOS", editorName);

        // Linux
        return Path.Combine(enginePath, "Engine", "Binaries", "Linux", editorName);
    }

    private static string ResolveRelativePath(string basePath, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return relativePath;
        return Path.GetFullPath(Path.Combine(basePath, relativePath));
    }

    private static string GetUProjectDirectory(string repoPath)
    {
        var uprojectFiles = Directory.GetFiles(repoPath, "*.uproject", SearchOption.TopDirectoryOnly);
        return uprojectFiles.Length > 0
            ? Path.GetDirectoryName(uprojectFiles[0]) ?? string.Empty
            : string.Empty;
    }

    private static IEnumerable<string> GetKnownInstallPaths(string versionStr)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var epicDir = Path.Combine(programFiles, "Epic Games");

            if (Directory.Exists(epicDir))
            {
                foreach (var dir in Directory.GetDirectories(epicDir, "UE_*"))
                {
                    if (string.IsNullOrEmpty(versionStr) || Path.GetFileName(dir).Contains(versionStr))
                        yield return dir;
                }
            }

            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? string.Empty;
            if (!string.IsNullOrEmpty(localAppData))
            {
                yield return Path.Combine(localAppData, "EpicGamesLauncher", "Engine");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, "Library", "Application Support", "Epic", "UnrealEngine");
            yield return Path.Combine("/Users", "Shared", "Epic Games");
        }
        else // Linux
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var epicDir = Path.Combine(home, "EpicGames");
            if (Directory.Exists(epicDir))
            {
                foreach (var dir in Directory.GetDirectories(epicDir, "UE_*"))
                    yield return dir;
            }
            yield return Path.Combine("/opt", "Epic Games");
        }
    }

    /// <summary>
    /// Look up an engine path from the Windows registry.
    ///
    /// GUID lookups: The GUID is a VALUE NAME (not a subkey) under:
    ///   HKCU\SOFTWARE\Epic Games\Unreal Engine\Builds
    ///   HKLM\SOFTWARE\Epic Games\Unreal Engine\Builds
    /// The engine path is the VALUE DATA.
    ///
    /// Version string lookups: Also stored as value names under the same Builds key.
    ///
    /// Fallback: version-named subkeys under SOFTWARE\Epic Games\Unreal Engine
    /// (e.g., HKCU\SOFTWARE\Epic Games\Unreal Engine\5.4 → InstalledDirectory)
    /// </summary>
    private static bool TryRegistryLookup(string key, out string path)
    {
        path = string.Empty;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        var hives = new[]
        {
            Microsoft.Win32.Registry.CurrentUser,
            Microsoft.Win32.Registry.LocalMachine
        };

        // Primary: GUID/version stored as a VALUE under the Builds key
        var buildsKeyPath = @"SOFTWARE\Epic Games\Unreal Engine\Builds";

        foreach (var hive in hives)
        {
            try
            {
                using var buildsKey = hive.OpenSubKey(buildsKeyPath);
                if (buildsKey == null) continue;

                if (!string.IsNullOrEmpty(key))
                {
                    // The association (GUID or version string) is the value name
                    var value = buildsKey.GetValue(key) as string;
                    if (!string.IsNullOrEmpty(value) && IsValidEngineRoot(value))
                    {
                        path = value;
                        return true;
                    }
                }
                else
                {
                    // Broad scan: check all values in the Builds key
                    foreach (var valueName in buildsKey.GetValueNames())
                    {
                        var value = buildsKey.GetValue(valueName) as string;
                        if (!string.IsNullOrEmpty(value) && IsValidEngineRoot(value))
                        {
                            path = value;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Registry access can fail due to permissions — continue
            }
        }

        // Fallback: scan version-named subkeys under the parent Unreal Engine key
        // (e.g. HKCU\SOFTWARE\Epic Games\Unreal Engine\5.4 → InstalledDirectory)
        var parentKeyPath = @"SOFTWARE\Epic Games\Unreal Engine";

        foreach (var hive in hives)
        {
            try
            {
                using var parentKey = hive.OpenSubKey(parentKeyPath);
                if (parentKey == null) continue;

                if (!string.IsNullOrEmpty(key))
                {
                    using var subKey = parentKey.OpenSubKey(key);
                    if (subKey != null)
                    {
                        var installedDir = subKey.GetValue("InstalledDirectory") as string;
                        if (!string.IsNullOrEmpty(installedDir) && IsValidEngineRoot(installedDir))
                        {
                            path = installedDir;
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (var subKeyName in parentKey.GetSubKeyNames())
                    {
                        using var subKey = parentKey.OpenSubKey(subKeyName);
                        if (subKey == null) continue;

                        var installedDir = subKey.GetValue("InstalledDirectory") as string;
                        if (!string.IsNullOrEmpty(installedDir) && IsValidEngineRoot(installedDir))
                        {
                            path = installedDir;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Registry access can fail — continue
            }
        }

        return false;
    }
}