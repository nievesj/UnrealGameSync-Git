using System.Diagnostics;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Abstraction for locating and launching the Unreal Editor associated with
/// a given engine installation.
/// Host implementation delegates to UGSGit.Services.EditorLauncher, which
/// resolves the editor binary path and starts the process.
/// </summary>
public interface IEditorLauncher
{
    /// <summary>
    /// Locate the Unreal Editor executable path for the configured engine.
    /// </summary>
    /// <returns>
    /// The absolute path to the Unreal Editor executable (e.g., Engine/Binaries/Win64/UnrealEditor.exe),
    /// or <c>null</c> if the editor binary cannot be found.
    /// </returns>
    string FindEditorPath();

    /// <summary>
    /// Launch the Unreal Editor with the specified project and optional command-line arguments.
    /// </summary>
    /// <param name="projectPath">Absolute path to the .uproject file to open in the editor.</param>
    /// <param name="arguments">Optional additional command-line arguments passed to the editor process (e.g., "-game", "-log").</param>
    /// <returns>A <see cref="Process"/> instance representing the launched editor process, or <c>null</c> if launch failed.</returns>
    Process Launch(string projectPath, string arguments = "");
}
