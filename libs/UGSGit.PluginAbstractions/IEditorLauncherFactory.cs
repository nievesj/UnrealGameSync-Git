namespace UGSGit.PluginAbstractions;

/// <summary>
/// Factory for creating <see cref="IEditorLauncher"/> instances bound to a
/// specific Unreal Engine installation.
/// Host implementation is UGSGit.Services.EditorLauncherFactory.
/// </summary>
public interface IEditorLauncherFactory
{
    /// <summary>
    /// Create an <see cref="IEditorLauncher"/> for the given engine installation.
    /// </summary>
    /// <param name="enginePath">Absolute path to the Unreal Engine root directory (e.g., the directory containing Engine/Build/Build.version).</param>
    /// <returns>An <see cref="IEditorLauncher"/> instance configured to locate and launch the editor for the specified engine.</returns>
    IEditorLauncher Create(string enginePath);
}
