namespace UGSGit.PluginAbstractions;

/// <summary>
/// A single annotation for a commit, displayed as a marker on the commit graph.
/// </summary>
/// <param name="Label">Short label shown in the badge (e.g. "Editor", "Code").</param>
/// <param name="Tooltip">Hover text (e.g. "Editor build available on network share").</param>
/// <param name="AnnotationType">Categorization key for host-side styling/theming (e.g. "build-available").</param>
/// <param name="Color">Optional hex color override (e.g. "#00FF00"). If null, the host uses AnnotationType theming.</param>
public record CommitAnnotation(string Label, string? Tooltip, string AnnotationType, string? Color = null);

/// <summary>
/// Well-known annotation type constants used by built-in plugins and the host.
/// </summary>
public static class CommitAnnotationTypes
{
    /// <summary>Indicates a precompiled editor build is available for this commit on the network share.</summary>
    public const string BuildAvailable = "build-available";

    /// <summary>Indicates a precompiled game build is available for this commit on the network share.</summary>
    public const string GameAvailable = "game-available";
}