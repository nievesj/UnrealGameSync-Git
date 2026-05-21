namespace UGSGit.PluginAbstractions;

/// <summary>
/// Result of a deploy operation (download + extract + manifest).
/// </summary>
public record DeployResult(DeployStatus Status, string Message);

/// <summary>
/// Status of a deploy operation.
/// </summary>
public enum DeployStatus
{
    /// <summary>Deploy completed successfully.</summary>
    Success,

    /// <summary>No build was found for the requested commit.</summary>
    NoBuildFound,

    /// <summary>The editor is running — cannot overwrite binaries.</summary>
    EditorRunning,

    /// <summary>The network share is unreachable.</summary>
    NetworkUnavailable,

    /// <summary>Deploy failed for another reason (see Message).</summary>
    Failed
}