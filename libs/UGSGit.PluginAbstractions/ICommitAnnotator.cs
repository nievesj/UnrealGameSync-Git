namespace UGSGit.PluginAbstractions;

/// <summary>
/// Implemented by plugins that want to annotate commits in the git graph.
/// The host calls AnnotateAsync() when generating the commit graph.
/// </summary>
public interface ICommitAnnotator
{
    /// <summary>
    /// Returns annotations for the given commits.
    /// Only called for commits currently visible in the graph (not the full history).
    /// </summary>
    /// <param name="commits">Commits to annotate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Map of short SHA → list of annotations. Commits with no annotation can be omitted.
    /// A commit can have multiple annotations (e.g., both code and content badges).</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>> AnnotateAsync(
        IReadOnlyList<CommitRef> commits, CancellationToken ct);
}