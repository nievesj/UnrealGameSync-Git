using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// User-local state persisted in .unrealsync/state.json (gitignored).
/// Mirrors UGS's .ugs/state.json convention.
/// </summary>
public class UgsWorkspaceState
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("lastSyncedCommit")]
    public string LastSyncedCommit { get; init; } = string.Empty;

    [JsonPropertyName("lastBuildCommit")]
    public string LastBuildCommit { get; init; } = string.Empty;

    [JsonPropertyName("selectedBuildTarget")]
    public string SelectedBuildTarget { get; init; } = string.Empty;

    [JsonPropertyName("afterSyncActions")]
    public UgsAfterSyncActions AfterSyncActions { get; init; } = new();

    [JsonPropertyName("archiveChannel")]
    public string ArchiveChannel { get; init; } = string.Empty;

    [JsonPropertyName("enginePathOverride")]
    public string EnginePathOverride { get; set; } = string.Empty;
}

public class UgsAfterSyncActions
{
    [JsonPropertyName("generateProjects")]
    public bool GenerateProjects { get; init; }

    [JsonPropertyName("build")]
    public bool Build { get; init; }

    [JsonPropertyName("run")]
    public bool Run { get; init; }

    [JsonPropertyName("openSolution")]
    public bool OpenSolution { get; init; }
}
