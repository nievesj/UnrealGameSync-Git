using System;

namespace UGSGit.PluginAbstractions
{
    /// <summary>
    /// Represents the result of attempting to load a plugin DLL.
    /// Can be success (manifest found and loaded), skipped (no IPluginManifest found),
    /// or failed (an exception occurred during loading).
    /// Factory methods <see cref="Success"/>, <see cref="Skipped"/>, and <see cref="Failed"/>
    /// provide convenient construction for each outcome.
    /// </summary>
    public class PluginLoadResult
    {
        /// <summary>The loaded manifest, or null if not successful.</summary>
        public IPluginManifest Manifest { get; init; }

        /// <summary>Path to the plugin DLL file.</summary>
        public string DllPath { get; init; }

        /// <summary>Whether the plugin loaded successfully.</summary>
        public bool IsSuccess { get; init; }

        /// <summary>Whether the DLL was skipped (no IPluginManifest found).</summary>
        public bool IsSkipped { get; init; }

        /// <summary>The exception that occurred during loading, or null.</summary>
        public Exception Error { get; init; }

        /// <summary>Human-readable error message describing what went wrong.</summary>
        public string ErrorMessage { get; init; }

        /// <summary>Creates a successful load result.</summary>
        /// <param name="manifest">The loaded plugin manifest.</param>
        /// <param name="dllPath">Path to the plugin DLL that was loaded.</param>
        /// <returns>A <see cref="PluginLoadResult"/> with <see cref="IsSuccess"/> set to true.</returns>
        public static PluginLoadResult Success(IPluginManifest manifest, string dllPath)
            => new() { Manifest = manifest, DllPath = dllPath, IsSuccess = true };

        /// <summary>Creates a skipped result for a DLL with no IPluginManifest.</summary>
        /// <param name="dllPath">Path to the skipped DLL file.</param>
        /// <param name="reason">Human-readable explanation of why the DLL was skipped.</param>
        /// <returns>A <see cref="PluginLoadResult"/> with <see cref="IsSkipped"/> set to true.</returns>
        public static PluginLoadResult Skipped(string dllPath, string reason)
            => new() { DllPath = dllPath, IsSkipped = true, ErrorMessage = reason };

        /// <summary>Creates a failed load result from an exception.</summary>
        /// <param name="dllPath">Path to the DLL that failed to load.</param>
        /// <param name="error">The exception that caused the failure.</param>
        /// <returns>A <see cref="PluginLoadResult"/> with <see cref="Error"/> and <see cref="ErrorMessage"/> populated.</returns>
        public static PluginLoadResult Failed(string dllPath, Exception error)
            => new() { DllPath = dllPath, Error = error, ErrorMessage = error.Message };
    }
}
