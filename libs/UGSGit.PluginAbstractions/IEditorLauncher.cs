using System.Diagnostics;

namespace UGSGit.Models;

/// <summary>
/// Editor launch abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.EditorLauncher.
/// </summary>
public interface IEditorLauncher
{
    string FindEditorPath();
    Process Launch(string projectPath, string arguments = "");
}
