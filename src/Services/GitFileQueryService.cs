using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Implements <see cref="IGitFileQueryService"/> using per-commit <c>git diff-tree</c> calls.
/// Handles root commits (via --root flag) and merge commits correctly.
/// Limits concurrent git.exe processes via a <see cref="SemaphoreSlim"/>.
/// </summary>
public class GitFileQueryService : IGitFileQueryService
{
    private readonly string _repoPath;
    private readonly SemaphoreSlim _concurrencyLimiter;

    /// <summary>
    /// Creates a new instance with the specified maximum concurrency.
    /// </summary>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    /// <param name="maxConcurrency">Maximum number of concurrent git.exe processes (default 5).</param>
    public GitFileQueryService(string repoPath, int maxConcurrency = 5)
    {
        _repoPath = repoPath;
        _concurrencyLimiter = new SemaphoreSlim(
            Math.Max(1, maxConcurrency),
            Math.Max(1, maxConcurrency));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetChangedFilesAsync(
        IReadOnlyList<string> commitShas, CancellationToken ct = default)
    {
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
    /// Runs <c>git diff-tree --no-commit-id -r --name-only --root &lt;sha&gt;</c>
    /// to get the list of files changed in a single commit.
    /// The --root flag handles root commits (which have no parent).
    /// Acquires the concurrency limiter before spawning the process.
    /// </summary>
    private async Task<(string Sha, IReadOnlyList<string> Files)> QueryCommitFilesAsync(
        string sha, CancellationToken ct)
    {
        await _concurrencyLimiter.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var proc = new Process();
            proc.StartInfo = CreateGitStartInfo(sha);

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
            _concurrencyLimiter.Release();
        }
    }

    private ProcessStartInfo CreateGitStartInfo(string sha)
    {
        var start = new ProcessStartInfo();
        start.FileName = Native.OS.GitExecutable;
        start.Arguments = $"--no-pager -c core.quotepath=off diff-tree --no-commit-id -r --name-only --root {sha}";
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
}
