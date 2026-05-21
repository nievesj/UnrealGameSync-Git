using System;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Git operations abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.GitSyncService.
/// Provides synchronous property access to the repo path and async methods for
/// common synchronisation and status-querying operations.
/// </summary>
public interface IGitSyncService
{
    /// <summary>Absolute path to the repository root.</summary>
    string RepoPath { get; }

    /// <summary>Gets the current branch name.</summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The current branch name, or an empty string if HEAD is detached.</returns>
    Task<string> GetCurrentBranchAsync(CancellationToken ct = default);

    /// <summary>Gets the current HEAD commit SHA.</summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The short form of the current HEAD commit SHA.</returns>
    Task<string> GetCurrentCommitAsync(CancellationToken ct = default);

    /// <summary>Syncs the repository to the latest commit on the specified branch.</summary>
    /// <param name="branch">The remote branch to sync to (e.g. "origin/main").</param>
    /// <param name="log">Progress reporter for sync status messages.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SyncResult"/> indicating success, conflict, or failure.</returns>
    Task<SyncResult> SyncToLatestAsync(string branch, IProgress<string> log, CancellationToken ct = default);

    /// <summary>Syncs the repository to a specific commit SHA.</summary>
    /// <param name="commitSha">The commit SHA to hard-reset to.</param>
    /// <param name="log">Progress reporter for sync status messages.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SyncResult"/> indicating success, conflict, or failure.</returns>
    Task<SyncResult> SyncToCommitAsync(string commitSha, IProgress<string> log, CancellationToken ct = default);

    /// <summary>Checks whether the working tree has uncommitted changes.</summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>True if there are uncommitted changes in the working tree; otherwise false.</returns>
    Task<bool> HasDirtyWorkingTreeAsync(CancellationToken ct = default);

    /// <summary>Checks whether the repository is locked by another process.</summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>True if the repository is locked; otherwise false.</returns>
    Task<bool> IsRepositoryLockedAsync(CancellationToken ct = default);
}

/// <summary>Outcome of a repository sync operation.</summary>
public enum SyncStatus
{
    /// <summary>The sync completed without conflicts or errors.</summary>
    Success,
    /// <summary>The sync encountered merge conflicts that require resolution.</summary>
    Conflict,
    /// <summary>The sync failed due to an error.</summary>
    Failed
}

/// <summary>Result of a repository sync operation.</summary>
/// <param name="Status">Outcome of the sync (Success, Conflict, or Failed).</param>
/// <param name="Message">Human-readable description of the sync result.</param>
/// <param name="CommitSha">The commit SHA that was synced to, if applicable. Empty string if not provided.</param>
/// <param name="Duration">The elapsed time the sync operation took, or null if not measured.</param>
public record SyncResult(SyncStatus Status, string Message, string CommitSha = "", TimeSpan? Duration = null);
