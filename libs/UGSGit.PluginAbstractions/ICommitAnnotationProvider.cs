namespace UGSGit.PluginAbstractions;

/// <summary>
/// Service that collects annotations from all registered plugin annotators.
/// Registered as a singleton — accessible via <see cref="PluginContext.GetService{T}"/>.
/// </summary>
public interface ICommitAnnotationProvider
{
    /// <summary>
    /// Gets annotations for the given commit SHAs from all registered annotators.
    /// Fans out to all annotators in parallel, merges results.
    /// If an annotator throws or times out, logs a warning and returns without that annotator's markers.
    /// </summary>
    /// <param name="commitShas">List of short commit SHAs visible in the graph.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Map of short SHA → list of annotations.</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>> GetAnnotationsAsync(
        IReadOnlyList<string> commitShas, CancellationToken ct);

    /// <summary>
    /// Registers a commit annotator from a plugin.
    /// Called when a repo tab is activated and the annotator is created.
    /// </summary>
    void Register(ICommitAnnotator annotator);

    /// <summary>
    /// Unregisters a commit annotator.
    /// Called when a repo tab is deactivated or disposed.
    /// </summary>
    void Unregister(ICommitAnnotator annotator);
}