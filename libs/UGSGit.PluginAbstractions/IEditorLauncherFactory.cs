namespace UGSGit.PluginAbstractions;

/// <summary>
/// Factory for creating IEditorLauncher instances with runtime parameters.
/// </summary>
public interface IEditorLauncherFactory
{
    IEditorLauncher Create(string enginePath);
}
