namespace UGSGit.PluginAbstractions;

/// <summary>
/// Process-wide concurrency limiter for all git.exe spawning.
/// Prevents CPU overload by bounding the total number of concurrent git processes
/// across all callers — annotation queries, build discovery, status checks, etc.
/// <para>
/// Use <see cref="AcquireAsync"/> to obtain a disposable token that releases the slot
/// on dispose. This ensures the release targets the same semaphore instance that
/// granted the slot, even if <see cref="UpdateMaxConcurrency"/> hot-swaps the underlying
/// semaphore between acquire and release.
/// </para>
/// </summary>
public static class GitProcessLimiter
{
    private static SemaphoreSlim _limiter = new(
        UgsConfig.DefaultMaxConcurrentGitProcesses,
        UgsConfig.DefaultMaxConcurrentGitProcesses);
    private static int _maxConcurrency = UgsConfig.DefaultMaxConcurrentGitProcesses;

    /// <summary>
    /// Acquires a concurrency slot. The returned <see cref="IDisposable"/> must be
    /// disposed (ideally via <c>using</c>) to release the slot. This guarantees
    /// the release targets the same semaphore instance that granted the slot,
    /// even if the semaphore is hot-swapped by <see cref="UpdateMaxConcurrency"/>
    /// between acquire and release.
    /// </summary>
    public static async Task<IDisposable> AcquireAsync(CancellationToken ct)
    {
        // Capture reference before awaiting — this is the semaphore we must release.
        var sem = Volatile.Read(ref _limiter);
        await sem.WaitAsync(ct).ConfigureAwait(false);
        return new Acquisition(sem);
    }

    /// <summary>
    /// Updates the maximum concurrency. In-flight requests continue on the old
    /// semaphore; new requests use the new limit. The old semaphore drains naturally
    /// as holders release their <see cref="Acquisition"/> tokens.
    /// </summary>
    public static void UpdateMaxConcurrency(int newMax)
    {
        newMax = Math.Clamp(newMax, 1, 20);
        var current = Volatile.Read(ref _maxConcurrency);
        if (newMax == current) return;

        var old = Interlocked.Exchange(ref _limiter,
            new SemaphoreSlim(newMax, newMax));
        Volatile.Write(ref _maxConcurrency, newMax);

        // Old semaphore drains as holders release their Acquisition tokens.
        // No explicit disposal needed — it becomes eligible for GC once drained.
    }

    /// <summary>
    /// Current maximum concurrency setting.
    /// </summary>
    public static int CurrentMaxConcurrency => Volatile.Read(ref _maxConcurrency);

    /// <summary>
    /// Disposable acquisition token that releases the specific semaphore instance
    /// that granted the slot. This prevents the race condition where
    /// <see cref="UpdateMaxConcurrency"/> swaps the semaphore between a
    /// <c>WaitAsync</c> and <c>Release</c>, which would over-release the new
    /// semaphore and leak a slot on the old one.
    /// </summary>
    private sealed class Acquisition : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Acquisition(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _semaphore, null)?.Release();
        }
    }
}