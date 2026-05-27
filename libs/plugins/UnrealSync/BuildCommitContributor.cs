using System;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;
using UGSGit.Plugins.UnrealSync.ViewModels;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Commit context menu contributor that adds a "Build" item to run all configured
/// build targets against the current workspace.
/// Long-running operation — displays a modal progress popup while building.
/// Only visible when the right-clicked commit is HEAD and build targets are configured.
/// Delegates to <see cref="FullWorkspaceViewModel.BuildAsync"/> which handles internal
/// log panel output via AppendLog.
/// </summary>
public class BuildCommitContributor : ICommitMenuContributor
{
    private FullWorkspaceViewModel? _vm;
    private readonly string _repoPath;

    /// <summary>
    /// Creates a new BuildCommitContributor for the specified repository.
    /// The ViewModel is set later via <see cref="SetViewModel"/> once it becomes available.
    /// This allows eager registration in the constructor so the menu item appears
    /// even before the UnrealSync tab has been activated.
    /// </summary>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    public BuildCommitContributor(string repoPath)
    {
        _repoPath = repoPath ?? throw new ArgumentNullException(nameof(repoPath));
    }

    /// <summary>
    /// Binds the contributor to a concrete workspace ViewModel.
    /// Called from <see cref="UnrealSyncTab.OnFullWorkspaceReady"/>.
    /// </summary>
    /// <param name="vm">The full workspace ViewModel providing build execution and state.</param>
    public void SetViewModel(FullWorkspaceViewModel vm)
    {
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
    }

    /// <inheritdoc/>
    public string Header => "Build";

    /// <inheritdoc/>
    public string? IconResourceKey => "Icons.Build";

    /// <inheritdoc/>
    public string RepoPath => _repoPath;

    /// <inheritdoc/>
    public bool RequiresBuildAnnotation => false;

    /// <inheritdoc/>
    public bool IsLongRunning => true;

    /// <inheritdoc/>
    public bool IsVisible(CommitRef commit)
    {
        // Show for all HEAD commits in UE repos (item may be disabled until VM binds)
        return commit.IsCurrentHead;
    }

    /// <inheritdoc/>
    public bool IsEnabled(CommitRef commit)
    {
        return commit.IsCurrentHead
            && _vm != null
            && _vm.BuildTargets.Count > 0
            && !_vm.IsBusy;
    }

    /// <inheritdoc/>
    public string? GroupKey => "UnrealSync";

    /// <inheritdoc/>
    public string? GroupHeader => "UnrealSync";

    /// <inheritdoc/>
    public string? GroupIconResourceKey => "Icons.UnrealSync";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CommitRef commit, IProgress<string>? log, CancellationToken ct)
    {
        if (_vm == null)
            throw new InvalidOperationException("Build contributor is not bound to a workspace ViewModel.");

        // Forward the host's progress reporter directly. FullWorkspaceViewModel.BuildAsync
        // handles internal log panel output via AppendLog. The external IProgress<string>
        // feeds the host's modal progress popup.
        await _vm.BuildAsync(log, ct);
    }
}
