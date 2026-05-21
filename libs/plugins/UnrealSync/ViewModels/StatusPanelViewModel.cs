using System;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync.ViewModels;

/// <summary>
/// Lightweight toolbar status display: shows current branch and commit info.
/// All Sync/Build/Launch operations are handled by FullWorkspaceViewModel.
/// Used as <c>ToolbarContent</c> in the UnrealSync tab.
/// This is a read-only status bar facade.
/// </summary>
public partial class StatusPanelViewModel : ObservableObject
{
    private readonly string _repoPath;
    private readonly IGitSyncService _syncService;

    /// <summary>
    /// Current status message displayed in the toolbar.
    /// </summary>
    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>
    /// Current branch name, or "unknown" if not yet fetched.
    /// </summary>
    [ObservableProperty]
    private string _branchText = "unknown";

    /// <summary>
    /// Current commit short hash, or "---" if not yet fetched.
    /// </summary>
    [ObservableProperty]
    private string _commitText = "---";

    /// <summary>
    /// Subject line of the current commit.
    /// </summary>
    [ObservableProperty]
    private string _commitSubject = "";

    /// <summary>
    /// Initializes a new instance of <see cref="StatusPanelViewModel"/>.
    /// </summary>
    /// <param name="repoPath">Absolute path to the repository root.</param>
    /// <param name="syncService">Git sync service for branch and commit queries.</param>
    public StatusPanelViewModel(string repoPath, IGitSyncService syncService)
    {
        _repoPath = repoPath;
        _syncService = syncService;
    }

    /// <summary>
    /// Fetches the current branch name and commit hash from the repository.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the refresh.</param>
    /// <returns>A task that completes when the branch and commit info have been fetched.</returns>
    public async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            BranchText = await _syncService.GetCurrentBranchAsync(ct);
            CommitText = await _syncService.GetCurrentCommitAsync(ct);
            StatusText = "Ready";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }
}
