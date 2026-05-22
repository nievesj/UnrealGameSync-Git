using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// User-local workspace state persisted in <c>.unrealsync/state.json</c> (gitignored).
/// Mirrors UGS's .ugs/state.json convention.
/// </summary>
public class UgsWorkspaceState
{
    /// <summary>State schema version number. Increment on breaking format changes.</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    /// <summary>SHA of the last commit that was synced to this workspace.</summary>
    [JsonPropertyName("lastSyncedCommit")]
    public string LastSyncedCommit { get; init; } = string.Empty;

    /// <summary>SHA of the last commit that was built in this workspace.</summary>
    [JsonPropertyName("lastBuildCommit")]
    public string LastBuildCommit { get; init; } = string.Empty;

    /// <summary>Identifier of the currently selected build target.</summary>
    [JsonPropertyName("selectedBuildTarget")]
    public string SelectedBuildTarget { get; init; } = string.Empty;

    /// <summary>Actions to perform after a sync operation.</summary>
    [JsonPropertyName("afterSyncActions")]
    public UgsAfterSyncActions AfterSyncActions { get; init; } = new();

    /// <summary>Override path to the Unreal Engine root, overrides auto-detected path.</summary>
    [JsonPropertyName("enginePathOverride")]
    public string EnginePathOverride { get; set; } = string.Empty;

    /// <summary>SHA of the last deployed editor binary archive (9-char short SHA).</summary>
    [JsonPropertyName("lastDeployedArchiveSha")]
    public string LastDeployedArchiveSha { get; set; } = string.Empty;
}

/// <summary>
/// Actions to perform after a sync operation.
/// </summary>
public class UgsAfterSyncActions
{
    /// <summary>Whether to regenerate project files after syncing.</summary>
    [JsonPropertyName("generateProjects")]
    public bool GenerateProjects { get; init; }

    /// <summary>Whether to run a build after syncing.</summary>
    [JsonPropertyName("build")]
    public bool Build { get; init; }

    /// <summary>Whether to launch the editor or game after syncing.</summary>
    [JsonPropertyName("run")]
    public bool Run { get; init; }

    /// <summary>Whether to open the solution file after syncing.</summary>
    [JsonPropertyName("openSolution")]
    public bool OpenSolution { get; init; }
}
