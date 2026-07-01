using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace SourceGit.Services;

/// <summary>
/// Implements <see cref="IGitFileQueryService"/> using per-commit <c>git diff-tree</c> calls.
/// Handles root commits (via --root flag) and merge commits correctly.
/// Limits concurrent git.exe processes via <see cref="GitProcessLimiter"/>.
/// </summary>
public class GitFileQueryService : IGitFileQueryService
{
    private static readonly Regex SShaPattern = new(@"^[0-9a-fA-F]{4,40}$", RegexOptions.Compiled);

    private readonly string _repoPath;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    public GitFileQueryService(string repoPath)
    {
        _repoPath = repoPath;
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
    /// Acquires the process-wide <see cref="GitProcessLimiter"/> before spawning the process.
    /// </summary>
    private async Task<(string Sha, IReadOnlyList<string> Files)> QueryCommitFilesAsync(
        string sha, CancellationToken ct)
    {
        // Validate SHA format to prevent argument injection (C1)
        if (!SShaPattern.IsMatch(sha))
            return (sha, Array.Empty<string>());

        using var _ = await GitProcessLimiter.AcquireAsync(ct).ConfigureAwait(false);
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
}
