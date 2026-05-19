using System;

namespace SourceGit.Models
{
    /// <summary>
    /// Represents the result of attempting to load a plugin DLL.
    /// Can be success, skipped (no IPluginManifest found), or failed (exception).
    /// </summary>
    public class PluginLoadResult
    {
        public IPluginManifest Manifest { get; init; }
        public string DllPath { get; init; }
        public bool IsSuccess { get; init; }
        public bool IsSkipped { get; init; }
        public Exception Error { get; init; }
        public string ErrorMessage { get; init; }

        public static PluginLoadResult Success(IPluginManifest manifest, string dllPath)
            => new() { Manifest = manifest, DllPath = dllPath, IsSuccess = true };

        public static PluginLoadResult Skipped(string dllPath, string reason)
            => new() { DllPath = dllPath, IsSkipped = true, ErrorMessage = reason };

        public static PluginLoadResult Failed(string dllPath, Exception error)
            => new() { DllPath = dllPath, Error = error, ErrorMessage = error.Message };
    }
}