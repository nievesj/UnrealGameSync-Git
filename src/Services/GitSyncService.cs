using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using SourceGit.Models;

namespace SourceGit.Services;

/// <summary>
/// Delegates git operations to SourceGit's Commands infrastructure to avoid
/// index.lock conflicts. Fixes RC-9.
///
/// TODO (Phase 2): This is a Phase 1 stub that runs raw "git" processes directly,
/// bypassing SourceGit's Commands infrastructure (command logging, lock management,
/// UI integration). Refactor to use SourceGit.Commands.* once we understand the
/// internal API and the repo is open.
/// </summary>
public class GitSyncService
{
    public enum SyncStatus { Success, Conflict, Failed }

    public record SyncResult(
        SyncStatus Status,
        string Message,
        string CommitSha = "",
        TimeSpan? Duration = null
    );

    private readonly string _repoPath;

    public GitSyncService(string repoPath)
    {
        _repoPath = repoPath;
    }

    /// <summary>
    /// Sync to latest on current branch using git pull --rebase.
    /// Checks for dirty working tree first.
    /// </summary>
    public async Task<SyncResult> SyncToLatestAsync(
        string branch, IProgress<string> log, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // Check for dirty working tree
        if (await HasDirtyWorkingTreeAsync(ct))
        {
            return new SyncResult(SyncStatus.Conflict,
                "Working tree is dirty. Commit or stash changes before syncing.");
        }

        // Run git pull --rebase via SourceGit's Commands
        try
        {
            // Use SourceGit's command runner pattern
            var result = await RunGitCommandAsync(
                $"pull --rebase origin {branch}", log, ct);

            sw.Stop();

            if (result.ExitCode == 0)
            {
                var commitSha = await GetCurrentCommitAsync(ct);
                return new SyncResult(SyncStatus.Success,
                    "Sync completed successfully.", commitSha, sw.Elapsed);
            }

            return new SyncResult(SyncStatus.Failed,
                $"Sync failed (exit code {result.ExitCode}): {result.StdErr}", "", sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new SyncResult(SyncStatus.Failed, "Sync cancelled.", "", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Native.OS.LogException(ex);
            return new SyncResult(SyncStatus.Failed, $"Sync error: {ex.Message}", "", sw.Elapsed);
        }
    }

    /// <summary>
    /// Checkout a specific commit. Refuses if working tree is dirty.
    /// </summary>
    public async Task<SyncResult> SyncToCommitAsync(
        string commitSha, IProgress<string> log, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        if (await HasDirtyWorkingTreeAsync(ct))
        {
            return new SyncResult(SyncStatus.Conflict,
                "Working tree is dirty. Commit or stash changes before syncing.");
        }

        try
        {
            var result = await RunGitCommandAsync(
                $"checkout {commitSha}", log, ct);

            sw.Stop();

            if (result.ExitCode == 0)
            {
                return new SyncResult(SyncStatus.Success,
                    $"Checked out commit {commitSha[..Math.Min(7, commitSha.Length)]}.", commitSha, sw.Elapsed);
            }

            return new SyncResult(SyncStatus.Failed,
                $"Checkout failed (exit code {result.ExitCode}): {result.StdErr}", "", sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new SyncResult(SyncStatus.Failed, "Checkout cancelled.", "", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Native.OS.LogException(ex);
            return new SyncResult(SyncStatus.Failed, $"Checkout error: {ex.Message}", "", sw.Elapsed);
        }
    }

    public async Task<string> GetCurrentBranchAsync(CancellationToken ct)
    {
        var result = await RunGitCommandAsync("rev-parse --abbrev-ref HEAD", new Progress<string>(_ => { }), ct);
        return result.ExitCode == 0 ? result.StdOut.Trim() : "unknown";
    }

    public async Task<string> GetCurrentCommitAsync(CancellationToken ct)
    {
        var result = await RunGitCommandAsync("rev-parse HEAD", new Progress<string>(_ => { }), ct);
        return result.ExitCode == 0 ? result.StdOut.Trim() : string.Empty;
    }

    private async Task<bool> HasDirtyWorkingTreeAsync(CancellationToken ct)
    {
        var result = await RunGitCommandAsync("status --porcelain", new Progress<string>(_ => { }), ct);
        return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StdOut);
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunGitCommandAsync(
        string arguments, IProgress<string> log, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _repoPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            log.Report(stdout);
            if (!string.IsNullOrWhiteSpace(stderr))
                log.Report(stderr);

            return (process.ExitCode, stdout, stderr);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            }
            throw; // rethrow to caller for proper error handling
        }
    }
}
