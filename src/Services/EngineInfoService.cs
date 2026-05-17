using System;
using System.IO;
using System.Text.Json;

using UGSGit.Models;

namespace UGSGit.Services;

/// <summary>
/// Detects engine version and build type from Engine/Build/Build.version.
/// Distinguishes between Source builds, Launcher installs, and Licensee builds.
/// </summary>
public static class EngineInfoService
{
    /// <summary>
    /// Read Engine/Build/Build.version JSON to determine engine version and build type.
    /// </summary>
    public static EngineInfo Detect(string enginePath)
    {
        var versionPath = Path.Combine(enginePath, "Engine", "Build", "Build.version");
        if (!File.Exists(versionPath))
            return new EngineInfo { Version = "Unknown", BuildType = "Unknown", IsSourceBuild = true };

        try
        {
            var json = File.ReadAllText(versionPath);
            var buildVersion = JsonSerializer.Deserialize(json, UnrealSyncJsonContext.Default.BuildVersion);
            if (buildVersion == null)
                return new EngineInfo { Version = "Unknown", BuildType = "Unknown", IsSourceBuild = true };

            var buildType = InferBuildType(enginePath, buildVersion);

            return new EngineInfo
            {
                MajorVersion = buildVersion.MajorVersion,
                MinorVersion = buildVersion.MinorVersion,
                PatchVersion = buildVersion.PatchVersion,
                Changelist = buildVersion.Changelist,
                Version = $"{buildVersion.MajorVersion}.{buildVersion.MinorVersion}.{buildVersion.PatchVersion}",
                BuildType = buildType,
                IsSourceBuild = buildType == "Source"
            };
        }
        catch (Exception)
        {
            return new EngineInfo { Version = "Unknown", BuildType = "Unknown", IsSourceBuild = true };
        }
    }

    /// <summary>
    /// Infer the build type by checking for source-build markers vs launcher markers.
    /// </summary>
    private static string InferBuildType(string enginePath, BuildVersion version)
    {
        var hasLicenseeFile = File.Exists(Path.Combine(enginePath, ".licensee"));
        var isLicenseeVersion = version.IsLicenseeVersion == 1;

        // Setup.bat or Setup.sh = built from GitHub source
        var hasSetupScript = File.Exists(Path.Combine(enginePath, "Setup.bat"))
            || File.Exists(Path.Combine(enginePath, "Setup.sh"));

        // GenerateProjectFiles in root = source build workflow
        var hasGenerateProjectFiles = File.Exists(Path.Combine(enginePath, "GenerateProjectFiles.bat"))
            || File.Exists(Path.Combine(enginePath, "GenerateProjectFiles.sh"));

        // .git directory = cloned from GitHub
        var hasGitDir = Directory.Exists(Path.Combine(enginePath, ".git"));

        // Epic Games Launcher install paths
        var isInLauncherPath = enginePath.Contains("Epic Games", StringComparison.OrdinalIgnoreCase)
            || enginePath.Contains("EpicGames", StringComparison.OrdinalIgnoreCase);

        // Epic internal / partner licensee builds
        if (hasLicenseeFile || isLicenseeVersion)
            return "Licensee";

        // Source build: has setup scripts, project generation, or git history
        if (hasSetupScript || hasGenerateProjectFiles || hasGitDir)
            return "Source";

        // Installed in Epic Games path without source markers = Launcher binary install
        if (isInLauncherPath)
            return "Launcher";

        // Default: if no markers, assume source (custom/unknown build)
        return "Source";
    }
}