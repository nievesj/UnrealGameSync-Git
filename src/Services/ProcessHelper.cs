using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SourceGit.Services;

/// <summary>
/// Shared process utilities: kill process trees portably, etc.
/// Consolidates duplicate KillProcess helpers (fixes C-2).
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// Kill a process and all its children. Best-effort, cross-platform.
    /// Uses OS-native tree kill where available with fallback commands.
    /// </summary>
    public static void KillProcessTree(Process process)
    {
        if (process == null) return;
        try
        {
            if (!process.HasExited)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.Kill(entireProcessTree: true);
                    // Fallback for deep trees that escape job objects
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = $"/T /F /PID {process.Id}",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        };
                        Process.Start(psi)?.WaitForExit(1000);
                    }
                    catch { /* best effort */ }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Kill parent first, then children
                    try { process.Kill(); } catch { /* best effort */ }
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "sh",
                            Arguments = $"-c \"pkill -TERM -P {process.Id}\"",
                            CreateNoWindow = true
                        })?.WaitForExit(1000);
                    }
                    catch { /* best effort */ }
                }
                else // Linux
                {
                    // Kill parent first, then the process group
                    try { process.Kill(); } catch { /* best effort */ }
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "sh",
                            Arguments = $"-c \"kill -TERM -{process.Id}\"",
                            CreateNoWindow = true
                        })?.WaitForExit(1000);
                    }
                    catch { /* best effort */ }
                }
            }
        }
        catch { /* best effort */ }
        // Note: callers own the process lifecycle and should Dispose the Process object.
        // Do NOT dispose here to avoid double-dispose (M1).
    }
}
