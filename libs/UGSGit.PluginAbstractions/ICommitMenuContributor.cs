using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Plugin-provided menu item for the commit graph right-click context menu.
/// IsVisible/IsEnabled are called synchronously on every right-click and must
/// return instantly. ExecuteAsync is called when the user clicks the item.
/// </summary>
public interface ICommitMenuContributor
{
    /// <summary>Menu item label (e.g. "Sync Editor").</summary>
    string Header { get; }

    /// <summary>
    /// Icon resource key from the host's icon dictionary (e.g. "Icons.Download").
    /// Return null for a text-only item.
    /// </summary>
    string? IconResourceKey { get; }

    /// <summary>
    /// Repository path this contributor is scoped to.
    /// The host uses this to filter contributors for the active repository.
    /// </summary>
    string RepoPath { get; }

    /// <summary>
    /// Should the item appear in the context menu for this commit?
    /// Called synchronously. Must be cheap — check cached state only.
    /// </summary>
    bool IsVisible(CommitRef commit);

    /// <summary>
    /// Is the item clickable (enabled) for this commit?
    /// Called synchronously. Must be cheap.
    /// </summary>
    bool IsEnabled(CommitRef commit);

    /// <summary>
    /// Executes the action. May be long-running (network download, extraction).
    /// The caller provides a cancellation token from the UI context.
    /// </summary>
    Task ExecuteAsync(CommitRef commit, CancellationToken ct);
}

/// <summary>
/// Service that collects commit menu contributors from all plugins.
/// Registered as a singleton — accessible via <see cref="PluginContext.GetService{T}"/>
/// and the host's HostServices class.
/// </summary>
public interface ICommitMenuContributorProvider
{
    /// <summary>
    /// Registers a commit menu contributor from a plugin.
    /// Called when a repo tab is activated and the contributor is created.
    /// </summary>
    void Register(ICommitMenuContributor contributor);

    /// <summary>
    /// Unregisters a commit menu contributor.
    /// Called when a repo tab is deactivated or disposed.
    /// </summary>
    void Unregister(ICommitMenuContributor contributor);

    /// <summary>
    /// Gets all registered contributors for the specified repository path.
    /// Returns a snapshot list — safe to iterate on the UI thread.
    /// </summary>
    IReadOnlyList<ICommitMenuContributor> GetContributorsForRepo(string repoPath);
}