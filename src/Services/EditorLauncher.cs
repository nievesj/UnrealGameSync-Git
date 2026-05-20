using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Finds and launches the Unreal Engine editor. Cross-platform.
/// Separate from BuildService to keep concerns clean.
/// </summary>
public class EditorLauncher : IEditorLauncher
{
    private readonly string _enginePath;

    public EditorLauncher(string enginePath)
    {
        _enginePath = enginePath;
    }

    public static string EditorBinaryName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "UnrealEditor.exe"
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? "UnrealEditor.app/Contents/MacOS/UnrealEditor"
            : "UnrealEditor";

    /// <summary>
    /// Find the editor executable for the given engine path.
    /// Tries UE5 first, falls back to UE4 (fixes DG-13).
    /// Throws FileNotFoundException if no editor binary is found.
    /// </summary>
    public string FindEditorPath()
    {
        var ue5Path = Path.Combine(_enginePath, "Engine", "Binaries",
            GetPlatformBinaryDir(), EditorBinaryName);

        if (File.Exists(ue5Path))
            return ue5Path;

        // Try UE4 fallback
        var ue4Name = EditorBinaryName.Replace("UnrealEditor", "UE4Editor");
        var ue4Path = Path.Combine(_enginePath, "Engine", "Binaries",
            GetPlatformBinaryDir(), ue4Name);

        if (File.Exists(ue4Path))
            return ue4Path;

        throw new FileNotFoundException($"Could not find editor binary in {_enginePath}");
    }

    /// <summary>
    /// Launch the editor with the given project and arguments.
    /// Throws FileNotFoundException if the editor binary is not found.
    /// Catches Win32Exception and rethrows as FileNotFoundException with context.
    /// </summary>
    public Process Launch(string projectPath, string arguments = "")
    {
        var editorPath = FindEditorPath();

        var args = $"\"{projectPath}\"";
        if (!string.IsNullOrEmpty(arguments))
            args += $" {arguments}";

        var psi = new ProcessStartInfo
        {
            FileName = editorPath,
            Arguments = args,
            UseShellExecute = true
        };

        try
        {
            return Process.Start(psi) ?? throw new FileNotFoundException(
                $"Editor process failed to start: {editorPath}");
        }
        catch (Win32Exception ex)
        {
            throw new FileNotFoundException(
                $"Editor could not be launched. Binary not found or inaccessible: {editorPath}", ex);
        }
    }

    private static string GetPlatformBinaryDir() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Win64"
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Mac"
        : "Linux";
}
