using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.Models;

/// <summary>
/// Build execution abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.BuildService.
/// </summary>
public interface IBuildService
{
    Task<BuildResult> ExecuteStepAsync(
        UgsBuildStep step,
        IProgress<string> log,
        CancellationToken ct = default,
        TimeSpan? timeout = null,
        string? archiveDir = null);

    Task<BuildResult> ExecuteAllAsync(
        List<UgsBuildStep> steps,
        IProgress<string> log,
        CancellationToken ct = default,
        string? archiveDir = null);
}
