using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Abstraction for executing UGS build steps against an Unreal Engine build environment.
/// Host implementation delegates to UGSGit.Services.BuildService, which invokes
/// UnrealBuildTool (UBT) and optionally creates build archives.
/// </summary>
public interface IBuildService
{
    /// <summary>
    /// Execute a single build step (build target, cook content, etc.)
    /// and return the combined result.
    /// </summary>
    /// <param name="step">The build step descriptor containing target, platform, configuration, and optional UAT command.</param>
    /// <param name="log">Progress reporter for streaming build log output.</param>
    /// <param name="ct">Cancellation token to abort the build early.</param>
    /// <param name="timeout">Optional maximum duration for the build step; null means no timeout.</param>
    /// <param name="archiveDir">
    /// Optional directory path for archiving build outputs (e.g., packaged binaries).
    /// When non-null, the service copies outputs here after a successful build.
    /// </param>
    /// <returns>A <see cref="BuildResult"/> indicating success/failure, elapsed time, and log output.</returns>
    Task<BuildResult> ExecuteStepAsync(
        UgsBuildStep step,
        IProgress<string> log,
        CancellationToken ct = default,
        TimeSpan? timeout = null,
        string? archiveDir = null);

    /// <summary>
    /// Execute a sequence of build steps in order and return an aggregated result.
    /// If any step fails, remaining steps are skipped.
    /// </summary>
    /// <param name="steps">Ordered list of build steps to execute.</param>
    /// <param name="log">Progress reporter for streaming build log output from all steps.</param>
    /// <param name="ct">Cancellation token to abort the entire sequence early.</param>
    /// <param name="archiveDir">
    /// Optional directory path for archiving build outputs across all steps.
    /// Passed to each individual step execution.
    /// </param>
    /// <returns>A <see cref="BuildResult"/> aggregated from all executed steps; contains the first failure if any.</returns>
    Task<BuildResult> ExecuteAllAsync(
        List<UgsBuildStep> steps,
        IProgress<string> log,
        CancellationToken ct = default,
        string? archiveDir = null);
}
