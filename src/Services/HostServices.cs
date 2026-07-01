using UGSGit.PluginAbstractions;

namespace SourceGit.Services;

/// <summary>
/// Singleton registry for cross-cutting host services that must be accessible
/// to all repositories and plugins, not scoped to a single PluginContext.
/// </summary>
public static class HostServices
{
    /// <summary>
    /// Collects annotations from all registered plugin annotators.
    /// Populated by PluginRegistry when manifests are registered.
    /// </summary>
    public static ICommitAnnotationProvider AnnotationProvider { get; } = new CommitAnnotationProvider();

    /// <summary>
    /// Collects commit context menu contributors from all active plugins.
    /// Populated by tabs when they are activated; depopulated when deactivated.
    /// </summary>
    public static ICommitMenuContributorProvider MenuContributors { get; } = new CommitMenuContributorProvider();
}
