using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Implements <see cref="IGitFileQueryService"/> using per-commit <c>git diff-tree</c> calls.
/// Handles root commits (via --root flag) and merge commits correctly.
/// Limits concurrent git.exe processes via a <see cref="SemaphoreSlim"/>.
/// </summary>
public class GitFileQueryService : IGitFileQueryService, IDisposable
{
    private static readonly Regex SShaPattern = new(@"^[0-9a-fA-F]{4,40}$", RegexOptions.Compiled);

    private readonly string _repoPath;
    private int _maxConcurrency;
    private SemaphoreSlim _concurrencyLimiter;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance with the specified maximum concurrency.
    /// </summary>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    /// <param name="maxConcurrency">Maximum number of concurrent git.exe processes (default <see cref="UgsConfig.DefaultMaxConcurrentGitProcesses"/>).</param>
    public GitFileQueryService(string repoPath, int maxConcurrency = UgsConfig.DefaultMaxConcurrentGitProcesses)
    {
        _repoPath = repoPath;
        _maxConcurrency = Math.Max(1, maxConcurrency);
        _concurrencyLimiter = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetChangedFilesAsync(
        IReadOnlyList<string> commitShas, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = new Dictionary<string, IReadOnlyList<string>>(commitShas.Count);

        // Run per-commit git diff-tree calls with bounded concurrency
        var tasks = new List<Task<(string Sha, IReadOnlyList<string> Files)>>(commitShas.Count);
        foreach (var sha in commitShas)
        {
            tasks.Add(QueryCommitFilesAsync(sha, ct));
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (var (sha, files) in results)
        {
            if (files.Count > 0)
                result[sha] = files;
        }

        return result;
    }

    /// <summary>
    /// Updates the concurrency limit. In-flight requests continue on the old semaphore;
    /// new requests use the new limit. The old semaphore is eligible for GC once
    /// in-flight requests release it.
    /// </summary>
    /// <param name="newMax">New maximum concurrency (clamped to 1–20).</param>
    public void UpdateConcurrency(int newMax)
    {
        newMax = Math.Clamp(newMax, 1, 20);
        if (newMax == _maxConcurrency) return;

        var oldLimiter = Interlocked.Exchange(ref _concurrencyLimiter,
            new SemaphoreSlim(newMax, newMax));
        _maxConcurrency = newMax;

        // Old semaphore will be collected once in-flight requests release it
    }

    /// <summary>
    /// Runs <c>git diff-tree --no-commit-id -r --name-only --root &lt;sha&gt;</c>
    /// to get the list of files changed in a single commit.
    /// The --root flag handles root commits (which have no parent).
    /// Acquires the concurrency limiter before spawning the process.
    /// </summary>
    private async Task<(string Sha, IReadOnlyList<string> Files)> QueryCommitFilesAsync(
        string sha, CancellationToken ct)
    {
        // Validate SHA format to prevent argument injection (C1)
        if (!SShaPattern.IsMatch(sha))
            return (sha, Array.Empty<string>());

        var limiter = _concurrencyLimiter; // Capture current reference for thread safety
        await limiter.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var proc = new Process();
            proc.StartInfo = CreateGitStartInfo(sha);

            // Kill process on cancellation (H2)
            using var registration = ct.Register(() =>
            {
                try { proc.Kill(); } catch { }
            });

            try
            {
                proc.Start();
            }
            catch (Exception)
            {
                return (sha, Array.Empty<string>());
            }

            var output = await proc.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await proc.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);

            if (proc.ExitCode != 0 || string.IsNullOrEmpty(output))
                return (sha, Array.Empty<string>());

            var files = new List<string>();
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                    files.Add(trimmed);
            }

            return (sha, files);
        }
        finally
        {
            limiter.Release();
        }
    }

    private ProcessStartInfo CreateGitStartInfo(string sha)
    {
        var start = new ProcessStartInfo();
        start.FileName = Native.OS.GitExecutable;
        // Use ArgumentList to prevent argument injection (C1)
        start.ArgumentList.Add("--no-pager");
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("core.quotepath=off");
        start.ArgumentList.Add("diff-tree");
        start.ArgumentList.Add("--no-commit-id");
        start.ArgumentList.Add("-r");
        start.ArgumentList.Add("--name-only");
        start.ArgumentList.Add("--root");
        start.ArgumentList.Add(sha);
        start.WorkingDirectory = _repoPath;
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.StandardOutputEncoding = Encoding.UTF8;
        start.StandardErrorEncoding = Encoding.UTF8;

        // Force using en_US.UTF-8 locale on Linux
        if (OperatingSystem.IsLinux())
        {
            start.Environment.Add("LANG", "C");
            start.Environment.Add("LC_ALL", "C");
        }

        return start;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _concurrencyLimiter.Dispose();
    }
}
