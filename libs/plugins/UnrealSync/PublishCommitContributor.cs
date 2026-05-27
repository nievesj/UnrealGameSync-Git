using System;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;
using UGSGit.Plugins.UnrealSync.ViewModels;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Commit context menu contributor that adds a "Publish" item to upload the
/// last packaged zip to the configured network share.
/// Only visible when the right-clicked commit is HEAD and a zip has already
/// been built (LastZipPath is set).
/// </summary>
public class PublishCommitContributor : ICommitMenuContributor
{
    private FullWorkspaceViewModel? _vm;
    private readonly string _repoPath;

    /// <summary>
    /// Creates a new PublishCommitContributor for the specified repository.
    /// The ViewModel is set later via <see cref="SetViewModel"/> once it becomes available.
    /// This allows eager registration in the constructor so the menu item appears
    /// even before the UnrealSync tab has been activated.
    /// </summary>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    public PublishCommitContributor(string repoPath)
    {
        _repoPath = repoPath ?? throw new ArgumentNullException(nameof(repoPath));
    }

    /// <summary>
    /// Binds the contributor to a concrete workspace ViewModel.
    /// Called from <see cref="UnrealSyncTab.OnFullWorkspaceReady"/>.
    /// </summary>
    /// <param name="vm">The full workspace ViewModel providing publish execution and state.</param>
    public void SetViewModel(FullWorkspaceViewModel vm)
    {
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
    }

    /// <inheritdoc/>
    public string Header => "Publish";

    /// <inheritdoc/>
    public string? IconResourceKey => "Icons.Upload";

    /// <inheritdoc/>
    public string RepoPath => _repoPath;

    /// <inheritdoc/>
    public bool RequiresBuildAnnotation => false;

    /// <inheritdoc/>
    public bool IsLongRunning => true;

    /// <inheritdoc/>
    public bool IsVisible(CommitRef commit)
    {
        // Show for all HEAD commits in UE repos (item may be disabled until VM binds or zip is ready)
        return commit.IsCurrentHead;
    }

    /// <inheritdoc/>
    public bool IsEnabled(CommitRef commit)
    {
        return commit.IsCurrentHead
            && _vm != null
            && _vm.CanPublish;
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
            throw new InvalidOperationException("Publish contributor is not bound to a workspace ViewModel.");

        await _vm.PublishAsync(log, ct);
    }
}
