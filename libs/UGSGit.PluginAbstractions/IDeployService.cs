namespace UGSGit.PluginAbstractions;

/// <summary>
/// Service for discovering, downloading, and deploying precompiled editor builds
/// from a network share to the local project directory.
/// Interface lives in PluginAbstractions for plugin access; implementation is host-only.
/// </summary>
public interface IDeployService
{
    /// <summary>
    /// Lists available editor builds on the network share.
    /// </summary>
    /// <param name="networkBase">Base network path (e.g. \\server\Builds\UnrealEngine\Project).</param>
    /// <param name="channel">Channel subdirectory (e.g. "Editor").</param>
    /// <param name="projectName">Project name used in zip filename pattern.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of available builds with short SHA, path, size, and modification time.</returns>
    /// <remarks>
    /// Partial failures (permission errors on individual zips) are logged as warnings
    /// and the unreadable builds are skipped. Only fatal failures (network share
    /// completely unreachable) cause an exception.
    /// </remarks>
    Task<IReadOnlyList<DeployBuildInfo>> DiscoverAsync(string networkBase, string channel, string projectName, CancellationToken ct);

    /// <summary>
    /// Finds the build matching the exact commit SHA.
    /// If multiple builds exist for the same commit, returns the most recent by LastModified.
    /// </summary>
    Task<DeployBuildInfo?> FindBuildForCommitAsync(string networkBase, string channel, string projectName, string commitSha, CancellationToken ct);

    /// <summary>
    /// Downloads and deploys an editor build for the given commit SHA.
    /// Handles: discovery, download, lock detection, old binary cleanup, extraction, manifest write, state update.
    /// </summary>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    /// <param name="networkBase">Base network path for builds.</param>
    /// <param name="channel">Channel subdirectory.</param>
    /// <param name="projectName">Project name for zip filename matching.</param>
    /// <param name="commitSha">Short or full commit SHA to match.</param>
    /// <param name="log">Progress logger.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<DeployResult> DeployAsync(string repoPath, string networkBase, string channel, string projectName, string commitSha, IProgress<string> log, CancellationToken ct);
}