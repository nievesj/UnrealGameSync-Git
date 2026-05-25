using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Full .unrealsync-settings.json config model (committed to repo root).
/// Uses named provider schema (not typed arrays) per RC-4 fix.
/// All nested types are immutable records with init-only properties (fixes M-2).
/// </summary>
public record class UgsConfig
{
    /// <summary>Config schema version number. Increment on breaking format changes.</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; } = 2;

    /// <summary>Engine path, targets, and project file configuration.</summary>
    [JsonPropertyName("engine")]
    public UgsEngineConfig Engine { get; init; } = new();

    /// <summary>Base URL or network path for downloading build artifacts.</summary>
    [JsonPropertyName("networkBase")]
    public string NetworkBase { get; init; } = string.Empty;

    /// <summary>Channel name for editor builds (subdirectory under network base).</summary>
    [JsonPropertyName("editorChannel")]
    public string EditorChannel { get; init; } = "Editor";

    /// <summary>Channel name for game builds (subdirectory under network base).</summary>
    [JsonPropertyName("gameChannel")]
    public string GameChannel { get; init; } = "Game";

    /// <summary>
    /// Base name used for published/downloaded zip files.
    /// Defaults to the .uproject filename if empty.
    /// Use this when the zip name differs from the project file name.
    /// </summary>
    [JsonPropertyName("binaryName")]
    public string BinaryName { get; init; } = string.Empty;

    /// <summary>Hex color for editor build badges (e.g. "#00FF00"). Empty = default green.</summary>
    [JsonPropertyName("editorBadgeColor")]
    public string EditorBadgeColor { get; init; } = string.Empty;

    /// <summary>Hex color for game build badges (e.g. "#FFA500"). Empty = default orange.</summary>
    [JsonPropertyName("gameBadgeColor")]
    public string GameBadgeColor { get; init; } = string.Empty;

    /// <summary>Hex color for commit-code badges (e.g. "#74B9FF"). Empty = theme default.</summary>
    [JsonPropertyName("commitCodeBadgeColor")]
    public string CommitCodeBadgeColor { get; init; } = string.Empty;

    /// <summary>Hex color for commit-content badges (e.g. "#A29BFF"). Empty = theme default.</summary>
    [JsonPropertyName("commitContentBadgeColor")]
    public string CommitContentBadgeColor { get; init; } = string.Empty;

    /// <summary>Code/content classification rules for commit type badges.</summary>
    [JsonPropertyName("changeType")]
    public UgsChangeTypeConfig? ChangeType { get; init; }

    /// <summary>
    /// Maximum number of concurrent git.exe processes for commit type annotation.
    /// Limits resource usage when fetching file lists for many commits.
    /// Default: 5.
    /// </summary>
    [JsonPropertyName("maxConcurrentGitProcesses")]
    public int MaxConcurrentGitProcesses { get; init; } = 5;

    /// <summary>Archive/packaging configuration (zip format, profiles, GitHub Actions).</summary>
    [JsonPropertyName("archive")]
    public UgsArchiveConfig Archive { get; init; } = new();

    /// <summary>Default build parameters (output directory).</summary>
    [JsonPropertyName("buildDefaults")]
    public UgsBuildDefaultsConfig BuildDefaults { get; init; } = new();

    /// <summary>Publish deployment configuration (atomic updates).</summary>
    [JsonPropertyName("publish")]
    public UgsPublishConfig Publish { get; init; } = new();
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
/// Archive/packaging configuration.
/// </summary>
public record class UgsArchiveConfig
{
    /// <summary>Naming template for zip archives. Supports {branch}, {target}, {platform}, {config}, {shortSha} variables.</summary>
    [JsonPropertyName("zipNaming")] public string ZipNaming { get; init; } = "{branch}-{target}-{platform}-{config}-{shortSha}.zip";
    /// <summary>Whether to exclude PDB/symbol files from archives.</summary>
    [JsonPropertyName("excludePdb")] public bool ExcludePdb { get; init; } = true;
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
/// Default build parameters.
/// </summary>
public record class UgsBuildDefaultsConfig
{
    /// <summary>Output directory for staged builds, relative to the project directory.</summary>
    [JsonPropertyName("outputDirectory")] public string OutputDirectory { get; init; } = "Saved/StagedBuilds";
}

/// <summary>
/// Publish deployment configuration.
/// </summary>
public record class UgsPublishConfig
{
    /// <summary>Whether to use atomic (all-or-nothing) publish updates.</summary>
    [JsonPropertyName("atomic")] public bool Atomic { get; init; } = true;
}

/// <summary>
/// Code/content classification rules for commit type badges.
/// Team-shared config (saved to .unrealsync-settings.json, committed to the repo).
/// </summary>
public record class UgsChangeTypeConfig
{
    /// <summary>File extensions to classify as code (in addition to built-in list).</summary>
    [JsonPropertyName("extraCodeExtensions")]
    public List<string> ExtraCodeExtensions { get; init; } = new();

    /// <summary>File extensions to remove from code classification (override built-in).</summary>
    [JsonPropertyName("excludeCodeExtensions")]
    public List<string> ExcludeCodeExtensions { get; init; } = new();

    /// <summary>Glob patterns for paths that should always be classified as content.</summary>
    [JsonPropertyName("forceContentPaths")]
    public List<string> ForceContentPaths { get; init; } = new();
}
