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
    /// Icon resource key from the host's icon dictionary (e.g. "Icons.Fetch").
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
    /// True if the action is long-running (network download, build, etc.) and
    /// should display a modal progress popup. False for fast actions
    /// (Process.Start, local file ops) that complete in &lt; 1 second.
    /// Default is true.
    /// </summary>
    bool IsLongRunning => true;

    /// <summary>
    /// True if the host should gate this item on the presence of a
    /// "build-available" annotation for the right-clicked commit.
    /// Set to false for actions that do not depend on prebuilt binaries
    /// (e.g. Launch Editor, which uses the local workspace).
    /// Default is true.
    /// </summary>
    bool RequiresBuildAnnotation => true;

    /// <summary>
    /// Executes the action with optional progress reporting and cancellation support.
    /// May be long-running (network download, extraction).
    /// </summary>
    /// <param name="commit">The target commit.</param>
    /// <param name="log">
    /// Optional progress reporter. Passes messages from the action so the host
    /// can display them in a progress popup. May be null — contributors must
    /// null-check before calling Report() and should still log to their own
    /// IPluginLogger for diagnostics.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(CommitRef commit, IProgress<string>? log, CancellationToken ct);

    /// <summary>
    /// Optional grouping key. When two or more visible contributors for the same
    /// repository share the same non-null/empty GroupKey, the host renders them
    /// as children of a single submenu. Null or empty means render as a flat
    /// top-level item (legacy behavior).
    /// </summary>
    string? GroupKey => null;

    /// <summary>
    /// Display label for the parent submenu when GroupKey is set.
    /// Only the GroupHeader of the first visible contributor in registration order
    /// within the group is used; others are silently ignored.
    /// Ignored when GroupKey is null/empty.
    /// </summary>
    string? GroupHeader => null;

    /// <summary>
    /// Icon resource key for the parent submenu when GroupKey is set.
    /// Only the GroupIconResourceKey of the first visible contributor in
    /// registration order within the group is used.
    /// Ignored when GroupKey is null/empty.
    /// </summary>
    string? GroupIconResourceKey => null;

    /// <summary>
    /// Executes the action with cancellation support only (no progress reporting).
    /// Default implementation bridges to the three-parameter overload with a null log.
    /// </summary>
    Task ExecuteAsync(CommitRef commit, CancellationToken ct)
        => ExecuteAsync(commit, null, ct);
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
