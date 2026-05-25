using System;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Process-wide concurrency limiter for all git.exe spawning.
/// Prevents CPU overload by bounding the total number of concurrent git processes
/// across all callers — annotation queries, build discovery, status checks, etc.
/// </summary>
public static class GitProcessLimiter
{
    private static SemaphoreSlim _limiter = new(
        UgsConfig.DefaultMaxConcurrentGitProcesses,
        UgsConfig.DefaultMaxConcurrentGitProcesses);
    private static int _maxConcurrency = UgsConfig.DefaultMaxConcurrentGitProcesses;

    /// <summary>
    /// Acquires a concurrency slot. Callers must release via <see cref="Release"/>
    /// in a finally block.
    /// </summary>
    public static async Task WaitAsync(CancellationToken ct)
    {
        await _limiter.WaitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases a concurrency slot acquired via <see cref="WaitAsync"/>.
    /// </summary>
    public static void Release()
    {
        _limiter.Release();
    }

    /// <summary>
    /// Updates the maximum concurrency. In-flight requests continue on the old
    /// semaphore; new requests use the new limit. The old semaphore is eligible
    /// for GC once in-flight requests release it.
    /// </summary>
    public static void UpdateMaxConcurrency(int newMax)
    {
        newMax = Math.Clamp(newMax, 1, 20);
        if (newMax == _maxConcurrency) return;

        var old = Interlocked.Exchange(ref _limiter,
            new SemaphoreSlim(newMax, newMax));
        _maxConcurrency = newMax;

        // Old semaphore will be collected once in-flight requests release it
    }

    /// <summary>
    /// Current maximum concurrency setting.
    /// </summary>
    public static int CurrentMaxConcurrency => _maxConcurrency;
}
