using System;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Services;

namespace SourceGit.ViewModels.Tabs.UnrealSync;

/// <summary>
/// Lightweight toolbar status display: shows current branch and commit info.
/// All Sync/Build/Launch operations are handled by FullWorkspaceViewModel.
/// This is a read-only status bar facade.
/// </summary>
public partial class StatusPanelViewModel : ObservableObject
{
    private readonly string _repoPath;
    private readonly GitSyncService _syncService;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _branchText = "unknown";

    [ObservableProperty]
    private string _commitText = "---";

    [ObservableProperty]
    private string _commitSubject = "";

    public StatusPanelViewModel(string repoPath, GitSyncService syncService)
    {
        _repoPath = repoPath;
        _syncService = syncService;
    }

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
