namespace UGSGit.PluginAbstractions
{
    /// <summary>
    /// Abstraction for global plugin state persistence.
    /// Implemented by ViewModels.Preferences so PluginRegistry doesn't depend on ViewModels directly.
    /// 
    /// NOTE: Per-repository plugin overrides are stored in RepositoryUIStates.PerRepoPluginOverrides
    /// and accessed directly by PluginRegistry — they do not flow through this interface.
    /// 
    /// This interface handles the global default enable/disable state for each plugin;
    /// per-repository overrides use a separate mechanism.
    /// </summary>
    public interface IPluginStateStore
    {
        /// <summary>Get global enable/disable state for a plugin, or null if not set</summary>
        /// <param name="pluginId">The plugin's unique identifier (e.g. "sourcegit.hello-world").</param>
        /// <returns>True if globally enabled, false if globally disabled, or null if no global preference is set.</returns>
        bool? GetGlobalState(string pluginId);

        /// <summary>Set global enable/disable state for a plugin</summary>
        /// <param name="pluginId">The plugin's unique identifier (e.g. "sourcegit.hello-world").</param>
        /// <param name="enabled">True to enable globally, false to disable globally.</param>
        void SetGlobalState(string pluginId, bool enabled);

        /// <summary>Persist all state changes to disk</summary>
        void Save();
    }
}
