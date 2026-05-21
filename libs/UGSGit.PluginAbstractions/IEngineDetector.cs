namespace UGSGit.PluginAbstractions;

/// <summary>
/// Engine detection abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.EngineDetector.
/// </summary>
public interface IEngineDetector
{
    /// <summary>
    /// Detects the Unreal Engine installation root path for the given project metadata.
    /// </summary>
    /// <param name="meta">Metadata describing the <c>.uproject</c> file, including the engine association string.</param>
    /// <param name="repoPath">Absolute path to the repository root, used to resolve relative engine references.</param>
    /// <returns>The absolute path to the engine root directory, or <c>null</c> if no suitable installation is found.</returns>
    string? Detect(UProjectMeta meta, string repoPath);

    /// <summary>
    /// Clears any cached engine detection results.
    /// Call this when engine installations may have changed.
    /// </summary>
    void ClearCache();
}
