using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UGSGit.Models;

/// <summary>
/// Full .unrealsync.json config model (committed to repo root).
/// Uses named provider schema (not typed arrays) per RC-4 fix.
/// All nested types are immutable records with init-only properties (fixes M-2).
/// </summary>
public record class UgsConfig
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("engine")]
    public UgsEngineConfig Engine { get; init; } = new();

    [JsonPropertyName("networkBase")]
    public string NetworkBase { get; init; } = string.Empty;

    [JsonPropertyName("sync")]
    public UgsSyncConfig Sync { get; init; } = new();

    [JsonPropertyName("archive")]
    public UgsArchiveConfig Archive { get; init; } = new();

    [JsonPropertyName("buildDefaults")]
    public UgsBuildDefaultsConfig BuildDefaults { get; init; } = new();

    [JsonPropertyName("publish")]
    public UgsPublishConfig Publish { get; init; } = new();

    [JsonPropertyName("ci")]
    public UgsCiConfig Ci { get; init; } = new();

    [JsonPropertyName("changeTypes")]
    public UgsChangeTypeConfig ChangeTypes { get; init; } = new();
}

public record class UgsEngineConfig
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("buildTargets")]
    public List<UgsBuildStep> BuildTargets { get; init; } = new();

    [JsonPropertyName("projectFile")]
    public string ProjectFile { get; init; } = string.Empty;

    [JsonPropertyName("editorArguments")]
    public string EditorArguments { get; init; } = string.Empty;

    [JsonPropertyName("autoDetect")]
    public bool AutoDetect { get; init; } = true;
}

public record class UgsSyncConfig
{
    [JsonPropertyName("defaultBranch")]
    public string DefaultBranch { get; init; } = string.Empty;

    [JsonPropertyName("autoRebase")]
    public bool AutoRebase { get; init; } = true;

    [JsonPropertyName("hooks")]
    public UgsSyncHooks Hooks { get; init; } = new();
}

public record class UgsSyncHooks
{
    [JsonPropertyName("preSync")]
    public List<string> PreSync { get; init; } = new();

    [JsonPropertyName("postCheckout")]
    public List<string> PostCheckout { get; init; } = new();
}

public record class UgsArchiveConfig
{
    [JsonPropertyName("enabled")] public bool Enabled { get; init; }
    [JsonPropertyName("channel")] public string Channel { get; init; } = "Editor";
    [JsonPropertyName("customChannel")] public string CustomChannel { get; init; } = string.Empty;
    [JsonPropertyName("zipNaming")] public string ZipNaming { get; init; } = "{branch}-{target}-{platform}-{config}-{shortSha}.zip";
    [JsonPropertyName("excludePdb")] public bool ExcludePdb { get; init; } = true;
    [JsonPropertyName("compressionLevel")] public string CompressionLevel { get; init; } = "Optimal";
    // Phase 3: GitHub Actions archive provider
    [JsonPropertyName("githubActions")] public UgsArchiveGitHubConfig GitHubActions { get; init; } = new();
    // Config-driven package profiles (fixes L-5)
    [JsonPropertyName("profiles")] public List<UgsPackageProfile> Profiles { get; init; } = new();
}

public record class UgsArchiveGitHubConfig
{
    [JsonPropertyName("repository")]
    public string Repository { get; init; } = string.Empty;

    [JsonPropertyName("workflow")]
    public string Workflow { get; init; } = string.Empty;

    [JsonPropertyName("artifactName")]
    public string ArtifactName { get; init; } = string.Empty;
}

public record class UgsCiConfig
{
    [JsonPropertyName("teamcity")]
    public UgsTeamCityConfig TeamCity { get; init; } = new();
}

public record class UgsTeamCityConfig
{
    [JsonPropertyName("serverUrl")]
    public string ServerUrl { get; init; } = string.Empty;

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("buildTypeId")]
    public string BuildTypeId { get; init; } = string.Empty;
}

public record class UgsChangeTypeConfig
{
    [JsonPropertyName("codeExtensions")]
    public List<string> CodeExtensions { get; init; } = new();

    [JsonPropertyName("codeExcludeFilter")]
    public List<string> CodeExcludeFilter { get; init; } = new();
}

public record class UgsBuildDefaultsConfig
{
    [JsonPropertyName("defaultConfig")] public string DefaultConfig { get; init; } = "Development";
    [JsonPropertyName("buildContentWhenPackaging")] public bool BuildContentWhenPackaging { get; init; }
    [JsonPropertyName("outputDirectory")] public string OutputDirectory { get; init; } = "Saved/StagedBuilds";
}

public record class UgsPublishConfig
{
    [JsonPropertyName("channel")] public string Channel { get; init; } = "Editor";
    [JsonPropertyName("customChannel")] public string CustomChannel { get; init; } = string.Empty;
    [JsonPropertyName("atomic")] public bool Atomic { get; init; } = true;
    [JsonPropertyName("deleteMissing")] public bool DeleteMissing { get; init; }
}
