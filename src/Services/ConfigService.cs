using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

using SourceGit.Models;

namespace SourceGit.Services;

/// <summary>
/// Reads and writes .unrealsync.json config files with environment variable expansion,
/// version migration, and user-local state persistence.
/// </summary>
public static class ConfigService
{
    private const string ConfigFileName = ".unrealsync.json";
    private const string LocalConfigDir = ".unrealsync";
    private const string LocalConfigFile = "local.json";

    private static readonly Regex EnvVarRegex = new(@"\$\{(\w+)\}", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static ConfigService()
    {
        JsonOptions.TypeInfoResolverChain.Add(UnrealSyncJsonContext.Default);
    }

    /// <summary>
    /// Load the team-shared config from repo root. Returns an empty config if file doesn't exist.
    /// </summary>
    public static UgsConfig LoadConfig(string repoPath)
    {
        var configPath = GetConfigPath(repoPath);
        if (!File.Exists(configPath))
            return new UgsConfig();

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize(json, UnrealSyncJsonContext.Default.UgsConfig)
            ?? new UgsConfig();

        // Validate version — silently fall back to default for unknown versions (fixes M-7)
        if (config.Version > 2)
            return new UgsConfig { Version = -1 };

        // Expand environment variables in all string fields (returns new config for immutability)
        config = ExpandEnvVarsInConfig(config);

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
    /// Load user-local state from .unrealsync/local.json (gitignored).
    /// </summary>
    public static UgsWorkspaceState LoadLocalState(string repoPath)
    {
        var localPath = GetLocalConfigPath(repoPath);
        if (!File.Exists(localPath))
            return new UgsWorkspaceState();

        var json = File.ReadAllText(localPath);
        return JsonSerializer.Deserialize(json, UnrealSyncJsonContext.Default.UgsWorkspaceState)
            ?? new UgsWorkspaceState();
    }

    /// <summary>
    /// Save user-local state to .unrealsync/local.json.
    /// </summary>
    public static void SaveLocalState(string repoPath, UgsWorkspaceState state)
    {
        var localPath = GetLocalConfigPath(repoPath);
        var dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(state, UnrealSyncJsonContext.Default.UgsWorkspaceState);
        File.WriteAllText(localPath, json);
    }

    /// <summary>
    /// Save the team-shared config to .unrealsync.json in the repo root.
    /// </summary>
    public static void SaveConfig(string repoPath, UgsConfig config)
    {
        var configPath = GetConfigPath(repoPath);
        var json = JsonSerializer.Serialize(config, UnrealSyncJsonContext.Default.UgsConfig);
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
        // Expand env vars in CI config (uses with expressions since config is now an immutable record)
        if (config.Ci?.TeamCity is { } tc && !string.IsNullOrEmpty(tc.AccessToken))
        {
            var resolved = ResolveEnvVars(tc.AccessToken);
            config = config with
            {
                Ci = config.Ci with
                {
                    TeamCity = tc with { AccessToken = resolved }
                }
            };
        }

        return config;
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
