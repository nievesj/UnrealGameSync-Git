using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Full .unrealsync.json config model (committed to repo root).
/// Uses named provider schema (not typed arrays) per RC-4 fix.
/// All nested types are immutable records with init-only properties (fixes M-2).
/// </summary>
public record class UgsConfig
{
    /// <summary>Config schema version number. Increment on breaking format changes.</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    /// <summary>Engine path, targets, and project file configuration.</summary>
    [JsonPropertyName("engine")]
    public UgsEngineConfig Engine { get; init; } = new();

    /// <summary>Base URL or network path for downloading build artifacts.</summary>
    [JsonPropertyName("networkBase")]
    public string NetworkBase { get; init; } = string.Empty;

    /// <summary>Sync behavior configuration (branch, rebase, hooks).</summary>
    [JsonPropertyName("sync")]
    public UgsSyncConfig Sync { get; init; } = new();

    /// <summary>Archive/packaging configuration (channels, zip format, GitHub Actions).</summary>
    [JsonPropertyName("archive")]
    public UgsArchiveConfig Archive { get; init; } = new();

    /// <summary>Default build parameters (config, output directory, content packaging).</summary>
    [JsonPropertyName("buildDefaults")]
    public UgsBuildDefaultsConfig BuildDefaults { get; init; } = new();

    /// <summary>Publish deployment configuration (channel, atomic updates, missing file handling).</summary>
    [JsonPropertyName("publish")]
    public UgsPublishConfig Publish { get; init; } = new();

    [JsonPropertyName("ci")]
    public UgsCiConfig Ci { get; init; } = new();

    [JsonPropertyName("changeTypes")]
    public UgsChangeTypeConfig ChangeTypes { get; init; } = new();
}

/// <summary>
/// Engine-related configuration (path, build targets, project file).
/// </summary>
public record class UgsEngineConfig
{
    /// <summary>File system path to the Unreal Engine root directory.</summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>List of build targets (editor, game, etc.) to compile.</summary>
    [JsonPropertyName("buildTargets")]
    public List<UgsBuildStep> BuildTargets { get; init; } = new();

    /// <summary>Project file (.uproject) relative to the repo root.</summary>
    [JsonPropertyName("projectFile")]
    public string ProjectFile { get; init; } = string.Empty;

    /// <summary>Additional arguments passed to the Unreal Editor process.</summary>
    [JsonPropertyName("editorArguments")]
    public string EditorArguments { get; init; } = string.Empty;

    /// <summary>Whether to auto-detect the engine path from the project association.</summary>
    [JsonPropertyName("autoDetect")]
    public bool AutoDetect { get; init; } = true;
}

/// <summary>
/// Sync behavior configuration.
/// </summary>
public record class UgsSyncConfig
{
    /// <summary>Default branch name to sync against (e.g. "main" or "develop").</summary>
    [JsonPropertyName("defaultBranch")]
    public string DefaultBranch { get; init; } = string.Empty;

    /// <summary>Whether to automatically rebase local changes after syncing.</summary>
    [JsonPropertyName("autoRebase")]
    public bool AutoRebase { get; init; } = true;

    /// <summary>Pre/post sync hook commands.</summary>
    [JsonPropertyName("hooks")]
    public UgsSyncHooks Hooks { get; init; } = new();
}

/// <summary>
/// Pre/post sync hook commands.
/// </summary>
public record class UgsSyncHooks
{
    /// <summary>Shell commands to run before sync starts.</summary>
    [JsonPropertyName("preSync")]
    public List<string> PreSync { get; init; } = new();

    /// <summary>Shell commands to run after checkout completes.</summary>
    [JsonPropertyName("postCheckout")]
    public List<string> PostCheckout { get; init; } = new();
}

/// <summary>
/// Archive/packaging configuration.
/// </summary>
public record class UgsArchiveConfig
{
    /// <summary>Whether archiving is enabled.</summary>
    [JsonPropertyName("enabled")] public bool Enabled { get; init; }
    /// <summary>Archive channel name (maps to named profile in archive provider).</summary>
    [JsonPropertyName("channel")] public string Channel { get; init; } = "Editor";
    /// <summary>Custom channel name override when channel is set to "Custom".</summary>
    [JsonPropertyName("customChannel")] public string CustomChannel { get; init; } = string.Empty;
    /// <summary>Naming template for zip archives. Supports {branch}, {target}, {platform}, {config}, {shortSha} variables.</summary>
    [JsonPropertyName("zipNaming")] public string ZipNaming { get; init; } = "{branch}-{target}-{platform}-{config}-{shortSha}.zip";
    /// <summary>Whether to exclude PDB/symbol files from archives.</summary>
    [JsonPropertyName("excludePdb")] public bool ExcludePdb { get; init; } = true;
    /// <summary>Compression level for zip archives ("Optimal", "Fastest", or "NoCompression").</summary>
    [JsonPropertyName("compressionLevel")] public string CompressionLevel { get; init; } = "Optimal";
    // Phase 3: GitHub Actions archive provider
    [JsonPropertyName("githubActions")] public UgsArchiveGitHubConfig GitHubActions { get; init; } = new();
    // Config-driven package profiles (fixes L-5)
    [JsonPropertyName("profiles")] public List<UgsPackageProfile> Profiles { get; init; } = new();
}

/// <summary>
/// GitHub Actions archive provider settings.
/// </summary>
public record class UgsArchiveGitHubConfig
{
    /// <summary>GitHub repository in "owner/repo" format.</summary>
    [JsonPropertyName("repository")]
    public string Repository { get; init; } = string.Empty;

    /// <summary>GitHub Actions workflow filename (e.g. "build.yml").</summary>
    [JsonPropertyName("workflow")]
    public string Workflow { get; init; } = string.Empty;

    /// <summary>Name of the artifact to download from the workflow run.</summary>
    [JsonPropertyName("artifactName")]
    public string ArtifactName { get; init; } = string.Empty;
}

/// <summary>
/// CI integration configuration.
/// </summary>
public record class UgsCiConfig
{
    [JsonPropertyName("teamcity")]
    public UgsTeamCityConfig TeamCity { get; init; } = new();
}

/// <summary>
/// TeamCity CI configuration.
/// </summary>
public record class UgsTeamCityConfig
{
    /// <summary>TeamCity server URL (e.g. "https://teamcity.example.com").</summary>
    [JsonPropertyName("serverUrl")]
    public string ServerUrl { get; init; } = string.Empty;

    /// <summary>TeamCity personal access token for API authentication.</summary>
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>TeamCity build configuration type ID used for triggering builds.</summary>
    [JsonPropertyName("buildTypeId")]
    public string BuildTypeId { get; init; } = string.Empty;
}

/// <summary>
/// File change type classification rules.
/// </summary>
public record class UgsChangeTypeConfig
{
    /// <summary>File extensions classified as code (e.g. ".cs", ".h", ".cpp").</summary>
    [JsonPropertyName("codeExtensions")]
    public List<string> CodeExtensions { get; init; } = new();

    /// <summary>Glob patterns to exclude from code classification (e.g. "**/Generated/**").</summary>
    [JsonPropertyName("codeExcludeFilter")]
    public List<string> CodeExcludeFilter { get; init; } = new();
}

/// <summary>
/// Default build parameters.
/// </summary>
public record class UgsBuildDefaultsConfig
{
    /// <summary>Default build configuration (e.g. "Development", "Shipping").</summary>
    [JsonPropertyName("defaultConfig")] public string DefaultConfig { get; init; } = "Development";
    /// <summary>Whether to build content when packaging (cook on the fly).</summary>
    [JsonPropertyName("buildContentWhenPackaging")] public bool BuildContentWhenPackaging { get; init; }
    /// <summary>Output directory for staged builds, relative to the project directory.</summary>
    [JsonPropertyName("outputDirectory")] public string OutputDirectory { get; init; } = "Saved/StagedBuilds";
}

/// <summary>
/// Publish deployment configuration.
/// </summary>
public record class UgsPublishConfig
{
    /// <summary>Publish channel name (maps to named profile in publish provider).</summary>
    [JsonPropertyName("channel")] public string Channel { get; init; } = "Editor";
    /// <summary>Custom channel name override when channel is set to "Custom".</summary>
    [JsonPropertyName("customChannel")] public string CustomChannel { get; init; } = string.Empty;
    /// <summary>Whether to use atomic (all-or-nothing) publish updates.</summary>
    [JsonPropertyName("atomic")] public bool Atomic { get; init; } = true;
    /// <summary>Whether to delete files on the server that no longer exist locally.</summary>
    [JsonPropertyName("deleteMissing")] public bool DeleteMissing { get; init; }
}
