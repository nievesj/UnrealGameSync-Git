namespace SourceGit.Models
{
    /// <summary>
    /// Abstraction for global plugin state persistence.
    /// Implemented by ViewModels.Preferences so PluginRegistry doesn't depend on ViewModels directly.
    /// 
    /// NOTE: Per-repository plugin overrides are stored in RepositoryUIStates.PerRepoPluginOverrides
    /// and accessed directly by PluginRegistry — they do not flow through this interface.
    /// </summary>
    public interface IPluginStateStore
    {
        /// <summary>Get global enable/disable state for a plugin, or null if not set</summary>
        bool? GetGlobalState(string pluginId);

        /// <summary>Set global enable/disable state for a plugin</summary>
        void SetGlobalState(string pluginId, bool enabled);

        /// <summary>Persist all state changes to disk</summary>
        void Save();
    }
}