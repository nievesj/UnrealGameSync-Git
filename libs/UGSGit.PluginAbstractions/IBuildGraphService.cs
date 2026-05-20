using System;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.Models;

/// <summary>
/// BuildGraph staging/zipping abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.BuildGraphService.
/// </summary>
public interface IBuildGraphService
{
    Task<StageResult> StageAsync(
        string editorTarget,
        string platform,
        string configuration,
        bool includePdb,
        IProgress<string> log,
        CancellationToken ct = default,
        TimeSpan? timeout = null);

    Task<string> CreateZipAsync(
        string stagingDir,
        string outputPath,
        bool excludePdb,
        IProgress<string> log,
        CancellationToken ct = default);
}
