#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

using UGSGit.Models;
using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Reads and writes .unrealsync-settings.json config files with environment variable expansion,
/// version migration, and user-local state persistence.
/// </summary>
public static class ConfigService
{
    private const string ConfigFileName = ".unrealsync-settings.json";
    private const string LocalConfigDir = ".unrealsync";
    private const string LocalConfigFile = "local-ue-path.json";

    private static readonly Regex EnvVarRegex = new(@"\$\{(\w+)\}", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static ConfigService()
    {
        JsonOptions.TypeInfoResolverChain.Add(PluginAbstractionsJsonContext.Default);
    }

    /// <summary>
    /// Load the team-shared config from repo root. Returns an empty config if file doesn't exist.
    /// </summary>
    public static UgsConfig LoadConfig(string repoPath)
    {
        var configPath = GetConfigPath(repoPath);
        if (!File.Exists(configPath))
        {
            // Try legacy filename (.unrealsync.json)
            var legacyPath = Path.Combine(repoPath, ".unrealsync.json");
            if (File.Exists(legacyPath))
                configPath = legacyPath;
            else
                return new UgsConfig();
        }

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize(json, PluginAbstractionsJsonContext.Default.UgsConfig)
            ?? new UgsConfig();

        // Validate version — for unknown future versions, preserve what we can and log a warning
        if (config.Version > 5)
        {
            Native.OS.LogException(new InvalidOperationException(
                $"UgsConfig version {config.Version} is newer than supported (max 5). Attempting to preserve known fields."));
        }

        // Migrate v2 → v3: commit type annotation fields added
        if (config.Version < 3)
        {
            config = config with
            {
                Version = 3,
                CommitCodeBadgeColor = config.CommitCodeBadgeColor ?? string.Empty,
                CommitContentBadgeColor = config.CommitContentBadgeColor ?? string.Empty,
                MaxConcurrentGitProcesses = config.MaxConcurrentGitProcesses > 0
                    ? config.MaxConcurrentGitProcesses : UgsConfig.DefaultMaxConcurrentGitProcesses
            };
        }

        // Migrate v3 → v4: BuildGraph script fields added
        if (config.Version < 4)
        {
            config = config with
            {
                Version = 4,
                BuildGraph = new UgsBuildGraphConfig(),
            };
        }

        // Migrate v4 → v5: logBatchSize field added to BuildGraph config
        if (config.Version < 5)
        {
            config = config with
            {
                Version = 5,
                BuildGraph = config.BuildGraph with
                {
                    LogBatchSize = config.BuildGraph.LogBatchSize > 0
                        ? config.BuildGraph.LogBatchSize : 50
                },
            };
        }

        // Expand environment variables in all string fields (returns new config for immutability)
        config = ExpandEnvVarsInConfig(config);

        // Clamp MaxConcurrentGitProcesses to valid range (1–20)
        if (config.MaxConcurrentGitProcesses < 1 || config.MaxConcurrentGitProcesses > 20)
        {
            config = config with
            {
                MaxConcurrentGitProcesses = Math.Clamp(config.MaxConcurrentGitProcesses, 1, 20)
            };
        }

        // Migrate build steps that lack BuildMode/UatCommand (Council F-3)
        if (config.Engine?.BuildTargets is { Count: > 0 })
        {
            var migrated = new List<UgsBuildStep>();
            foreach (var step in config.Engine.BuildTargets)
                migrated.Add(MigrateBuildStep(step));
            config = config with
            {
                Engine = config.Engine with { BuildTargets = migrated }
            };
        }

        return config;
    }

    /// <summary>
    /// Load user-local state from .unrealsync/local-ue-path.json (gitignored).
    /// </summary>
    public static UgsWorkspaceState LoadLocalState(string repoPath)
    {
        var localPath = GetLocalConfigPath(repoPath);
        if (!File.Exists(localPath))
        {
            // Try legacy filename (local.json)
            var legacyLocalPath = Path.Combine(repoPath, LocalConfigDir, "local.json");
            if (File.Exists(legacyLocalPath))
                localPath = legacyLocalPath;
            else
                return new UgsWorkspaceState();
        }

        var json = File.ReadAllText(localPath);
        return JsonSerializer.Deserialize(json, PluginAbstractionsJsonContext.Default.UgsWorkspaceState)
            ?? new UgsWorkspaceState();
    }

    /// <summary>
    /// Save user-local state to .unrealsync/local-ue-path.json.
    /// </summary>
    public static void SaveLocalState(string repoPath, UgsWorkspaceState state)
    {
        var localPath = GetLocalConfigPath(repoPath);
        var dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(state, PluginAbstractionsJsonContext.Default.UgsWorkspaceState);
        File.WriteAllText(localPath, json);
    }

    /// <summary>
    /// Save the team-shared config to .unrealsync-settings.json in the repo root.
    /// </summary>
    public static void SaveConfig(string repoPath, UgsConfig config)
    {
        var configPath = GetConfigPath(repoPath);
        var json = JsonSerializer.Serialize(config, PluginAbstractionsJsonContext.Default.UgsConfig);
        File.WriteAllText(configPath, json);
    }

    /// <summary>
    /// Resolve a ${NAME} environment variable reference. Unknown vars return empty string.
    /// </summary>
    public static string ResolveEnvVars(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return EnvVarRegex.Replace(value, match =>
            Environment.GetEnvironmentVariable(match.Groups[1].Value) ?? string.Empty);
    }

    internal static string GetConfigPath(string repoPath) =>
        Path.Combine(repoPath, ConfigFileName);

    internal static string GetLocalConfigPath(string repoPath) =>
        Path.Combine(repoPath, LocalConfigDir, LocalConfigFile);

    private static UgsConfig ExpandEnvVarsInConfig(UgsConfig config)
    {
        // Validate and normalize UgsChangeTypeConfig
        var changeType = NormalizeChangeTypeConfig(config.ChangeType);

        return config with
        {
            NetworkBase = ResolveEnvVars(config.NetworkBase),
            BinaryName = ResolveEnvVars(config.BinaryName),
            EditorBadgeColor = ResolveEnvVars(config.EditorBadgeColor),
            GameBadgeColor = ResolveEnvVars(config.GameBadgeColor),
            CommitCodeBadgeColor = ResolveEnvVars(config.CommitCodeBadgeColor),
            CommitContentBadgeColor = ResolveEnvVars(config.CommitContentBadgeColor),
            ChangeType = changeType,
            Engine = config.Engine with
            {
                Path = ResolveEnvVars(config.Engine.Path),
                ProjectFile = ResolveEnvVars(config.Engine.ProjectFile),
                EditorArguments = ResolveEnvVars(config.Engine.EditorArguments),
            },
            Archive = config.Archive with
            {
                ZipNaming = ResolveEnvVars(config.Archive.ZipNaming),
            },
            BuildDefaults = config.BuildDefaults with
            {
                OutputDirectory = ResolveEnvVars(config.BuildDefaults.OutputDirectory),
            },
            BuildGraph = config.BuildGraph with
            {
                EditorScript = ResolveEnvVars(config.BuildGraph.EditorScript),
                EditorTarget = ResolveEnvVars(config.BuildGraph.EditorTarget),
                GameScript = ResolveEnvVars(config.BuildGraph.GameScript),
                GameTarget = ResolveEnvVars(config.BuildGraph.GameTarget),
                ServerScript = ResolveEnvVars(config.BuildGraph.ServerScript),
                ServerTarget = ResolveEnvVars(config.BuildGraph.ServerTarget),
                SetArgsTemplate = ResolveEnvVars(config.BuildGraph.SetArgsTemplate),
            },
        };
    }

    /// <summary>
    /// Normalizes UgsChangeTypeConfig: ensures extensions start with ".",
    /// normalizes path separators to forward slashes, and wraps parsing in try/catch.
    /// </summary>
    private static UgsChangeTypeConfig? NormalizeChangeTypeConfig(UgsChangeTypeConfig? config)
    {
        if (config == null)
            return null;

        try
        {
            var extraCode = config.ExtraCodeExtensions ?? new List<string>();
            var excludeCode = config.ExcludeCodeExtensions ?? new List<string>();
            var forceContentPaths = config.ForceContentPaths ?? new List<string>();

            // Ensure extensions start with "."
            var normalizedExtra = extraCode.Select(e => e.StartsWith(".") ? e : "." + e).ToList();
            var normalizedExclude = excludeCode.Select(e => e.StartsWith(".") ? e : "." + e).ToList();

            // Normalize path separators to forward slashes
            var normalizedPaths = forceContentPaths.Select(p => p.Replace('\\', '/')).ToList();

            return new UgsChangeTypeConfig
            {
                ExtraCodeExtensions = normalizedExtra,
                ExcludeCodeExtensions = normalizedExclude,
                ForceContentPaths = normalizedPaths,
            };
        }
        catch (Exception ex)
        {
            // Malformed config — fall back to defaults
            System.Diagnostics.Debug.WriteLine(
                $"Failed to normalize UgsChangeTypeConfig, falling back to defaults: {ex.Message}");
            return new UgsChangeTypeConfig();
        }
    }

    /// <summary>
    /// Migrate a build step that lacks BuildMode/UatCommand (from old configs).
    /// Uses filename-only comparison to detect RunUAT (Council F-3).
    /// </summary>
    private static UgsBuildStep MigrateBuildStep(UgsBuildStep step)
    {
        if (!string.IsNullOrEmpty(step.BuildMode))
            return step;  // Already migrated

        var filename = Path.GetFileNameWithoutExtension(step.ScriptPath ?? "");
        var buildMode = filename.Equals("RunUAT", StringComparison.OrdinalIgnoreCase)
            ? BuildModes.Uat
            : BuildModes.Ubt;

        return step with
        {
            BuildMode = buildMode,
            UatCommand = buildMode == BuildModes.Uat ? "BuildCookRun" : null
        };
    }
}
