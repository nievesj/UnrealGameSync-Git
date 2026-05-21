using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

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
}
