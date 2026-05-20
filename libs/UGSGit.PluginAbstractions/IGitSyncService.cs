using System;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Git operations abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.GitSyncService.
/// </summary>
public interface IGitSyncService
{
    string RepoPath { get; }
    Task<string> GetCurrentBranchAsync(CancellationToken ct = default);
    Task<string> GetCurrentCommitAsync(CancellationToken ct = default);
    Task<SyncResult> SyncToLatestAsync(string branch, IProgress<string> log, CancellationToken ct = default);
    Task<SyncResult> SyncToCommitAsync(string commitSha, IProgress<string> log, CancellationToken ct = default);
    Task<bool> HasDirtyWorkingTreeAsync(CancellationToken ct = default);
    Task<bool> IsRepositoryLockedAsync(CancellationToken ct = default);
}

public enum SyncStatus { Success, Conflict, Failed }

public record SyncResult(SyncStatus Status, string Message, string CommitSha = "", TimeSpan? Duration = null);
