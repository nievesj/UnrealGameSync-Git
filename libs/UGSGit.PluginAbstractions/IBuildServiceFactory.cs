namespace UGSGit.PluginAbstractions;

/// <summary>
/// Factory for creating <see cref="IBuildService"/> instances bound to a specific
/// Unreal Engine installation and project file.
/// Host implementation is UGSGit.Services.BuildServiceFactory.
/// </summary>
public interface IBuildServiceFactory
{
    /// <summary>
    /// Create an <see cref="IBuildService"/> for the given engine and project.
    /// </summary>
    /// <param name="enginePath">Absolute path to the Unreal Engine root directory (e.g., the directory containing Engine/Build/Build.version).</param>
    /// <param name="uprojectPath">Absolute path to the .uproject file to build against.</param>
    /// <returns>An <see cref="IBuildService"/> instance configured for the specified engine and project.</returns>
    IBuildService Create(string enginePath, string uprojectPath);
}
