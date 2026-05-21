namespace UGSGit.PluginAbstractions;

/// <summary>
/// Factory for creating <see cref="IBuildGraphService"/> instances bound to a specific
/// Unreal Engine installation and workspace configuration.
/// Host implementation is UGSGit.Services.BuildGraphServiceFactory.
/// The repo path is not exposed in <c>Create</c> since it is derivable from
/// <c>PluginContext.RepositoryPath</c> at plugin activation time.
/// </summary>
public interface IBuildGraphServiceFactory
{
    /// <summary>
    /// Create an <see cref="IBuildGraphService"/> for the given engine and config.
    /// </summary>
    /// <param name="enginePath">Absolute path to the Unreal Engine root directory (e.g., the directory containing Engine/Build/Build.version).</param>
    /// <param name="config">The <see cref="UgsConfig"/> containing workspace-level build settings such as default editor target and platform.</param>
    /// <returns>An <see cref="IBuildGraphService"/> instance configured for the specified engine and config.</returns>
    IBuildGraphService Create(string enginePath, UgsConfig config);
}
