using System.Collections.Generic;

namespace UGSGit.PluginAbstractions
{
    /// <summary>
    /// Interface for plugin manifests. Every external plugin DLL must contain a class
    /// implementing this interface — it is the entry point for discovery.
    /// </summary>
    public interface IPluginManifest
    {
        /// <summary>Unique identifier, e.g. "sourcegit.hello-world". Must be stable across versions.</summary>
        string PluginId { get; }

        /// <summary>Display name, e.g. "Hello World"</summary>
        string DisplayName { get; }

        /// <summary>Short description for the settings UI</summary>
        string Description { get; }

        /// <summary>Version string, e.g. "1.0.0"</summary>
        string Version { get; }

        /// <summary>Author name</summary>
        string Author { get; }

        /// <summary>Whether this plugin is enabled in all repos by default</summary>
        bool IsGlobalByDefault { get; }

        /// <summary>SortOrder hint for plugin tabs (0 = first, 1000+ = after built-ins)</summary>
        int DefaultSortOrder { get; }

        /// <summary>Creates the tab instances for this plugin. May return multiple tabs.</summary>
        /// <remarks>Called on the UI thread. Exceptions are caught and result in an ErrorTab.</remarks>
        IReadOnlyList<IRepositoryTab> CreateTabs(PluginContext context);
    }
}
