using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

using UGSGit.PluginAbstractions;

namespace UGSGit.Models
{
    /// <summary>
    /// Singleton that holds all discovered plugin manifests and manages enabled/disabled state.
    /// Supports three-tier resolution: per-repo override → global default → manifest default.
    /// Thread-safe for concurrent reads and writes to shared collections.
    /// 
    /// Uses IPluginStateStore to avoid direct dependency on ViewModels.Preferences.
    /// </summary>
    public class PluginRegistry : ObservableObject
    {
        public static readonly PluginRegistry Instance = new();

        /// <summary>Injected state store — set by Preferences during initialization</summary>
        public IPluginStateStore StateStore { get; set; }

        /// <summary>
        /// All discovered plugins (including failed/skipped).
        /// DESIGN DECISION: Written once on the UI thread during startup (App.TryLaunchAsNormal)
        /// before any consumer exists. All reads happen after the single write is complete.
        /// No lock needed as long as this single-write-at-startup pattern is maintained.
        /// If hot-reload is added, this will need synchronization.
        /// </summary>
        public AvaloniaList<PluginLoadResult> DiscoveredPlugins { get; } = new();

        /// <summary>Successfully loaded manifests</summary>
        public IEnumerable<IPluginManifest> LoadedManifests =>
            DiscoveredPlugins.Where(r => r.IsSuccess).Select(r => r.Manifest);

        /// <summary>Built-in manifests (not from external DLLs). Exposed for UI enumeration.</summary>
        public IReadOnlyList<IPluginManifest> BuiltInManifests
        {
            get
            {
                lock (_lock)
                {
                    return _builtInManifests.ToArray();
                }
            }
        }

        /// <summary>Register a built-in plugin manifest with duplicate-check</summary>
        public bool RegisterBuiltInManifest(IPluginManifest manifest)
        {
            lock (_lock)
            {
                if (_builtInManifests.Any(m => m.PluginId == manifest.PluginId))
                {
                    Native.OS.LogException(new InvalidOperationException(
                        $"Duplicate built-in PluginId '{manifest.PluginId}' from {manifest.DisplayName}"));
                    return false;
                }
                _builtInManifests.Add(manifest);
            }
            return true;
        }

        /// <summary>
        /// All enabled manifests (built-in + successfully loaded external).
        /// NOTE: Enumerates DiscoveredPlugins without a lock — safe because DiscoveredPlugins
        /// is written once at startup and never modified afterward (see DiscoveredPlugins doc).
        /// </summary>
        public IEnumerable<IPluginManifest> AllManifests
        {
            get
            {
                IPluginManifest[] builtIns;
                lock (_lock)
                {
                    builtIns = _builtInManifests.ToArray();
                }
                foreach (var m in builtIns)
                    yield return m;
                foreach (var r in DiscoveredPlugins)
                    if (r.IsSuccess)
                        yield return r.Manifest;
            }
        }

        /// <summary>
        /// Thread-safe event raised when a plugin's enabled state changes.
        /// Arguments: pluginId — the plugin whose state changed.
        /// </summary>
        public event Action<string> PluginStateChanged
        {
            add { lock (_eventLock) { _pluginStateChanged += value; } }
            remove { lock (_eventLock) { _pluginStateChanged -= value; } }
        }

        /// <summary>
        /// Is the given plugin enabled for the specified repository?
        /// Resolution: per-repo override → global default → manifest's IsGlobalByDefault.
        /// </summary>
        public bool IsEnabledForRepository(string pluginId, RepositoryUIStates uiStates)
        {
            if (StateStore == null)
                return false;

            // Per-repo override takes priority
            if (uiStates != null && uiStates.SafePerRepoPluginOverrides.TryGetValue(pluginId, out var perRepoEnabled))
                return perRepoEnabled;

            return IsEnabledGlobally(pluginId);
        }

        /// <summary>
        /// Is the given plugin enabled globally?
        /// Resolution: global override → manifest's IsGlobalByDefault.
        /// </summary>
        public bool IsEnabledGlobally(string pluginId)
        {
            if (StateStore == null)
                return false;

            var manifest = AllManifests.FirstOrDefault(m => m.PluginId == pluginId);
            if (manifest == null)
                return false;

            var globalState = StateStore.GetGlobalState(pluginId);
            if (globalState.HasValue)
                return globalState.Value;

            return manifest.IsGlobalByDefault;
        }

        /// <summary>Get per-repository override state for a plugin, or null if inheriting</summary>
        public bool? GetPerRepositoryOverride(string pluginId, RepositoryUIStates uiStates)
        {
            if (uiStates != null && uiStates.SafePerRepoPluginOverrides.TryGetValue(pluginId, out var enabled))
                return enabled;
            return null;
        }

        /// <summary>Set per-repository enable/disable override</summary>
        public void SetEnabledForRepository(string pluginId, RepositoryUIStates uiStates, bool enabled)
        {
            if (uiStates == null || StateStore == null)
                return;

            uiStates.SafePerRepoPluginOverrides[pluginId] = enabled;
            uiStates.Save();
            RaisePluginStateChanged(pluginId);
        }

        /// <summary>Set global default enable/disable</summary>
        public void SetGlobalDefault(string pluginId, bool enabled)
        {
            if (StateStore == null)
                return;

            StateStore.SetGlobalState(pluginId, enabled);
            StateStore.Save();
            RaisePluginStateChanged(pluginId);
        }

        /// <summary>Remove per-repository override (revert to global default)</summary>
        public void ResetForRepository(string pluginId, RepositoryUIStates uiStates)
        {
            if (uiStates == null || StateStore == null)
                return;

            if (uiStates.SafePerRepoPluginOverrides.Remove(pluginId))
            {
                uiStates.Save();
                RaisePluginStateChanged(pluginId);
            }
        }

        /// <summary>Check whether a plugin has been activated for a given repository before</summary>
        public bool HasBeenActivated(string pluginId, string repositoryPath)
        {
            lock (_lock)
            {
                return _activatedPlugins.Contains((pluginId, repositoryPath));
            }
        }

        /// <summary>Mark a plugin as having been activated for a given repository</summary>
        public void MarkActivated(string pluginId, string repositoryPath)
        {
            lock (_lock)
            {
                _activatedPlugins.Add((pluginId, repositoryPath));
            }
        }

        /// <summary>Clear all activation records for a given repository (call when repo is closed)</summary>
        public void ClearActivationForRepository(string repositoryPath)
        {
            lock (_lock)
            {
                _activatedPlugins.RemoveWhere(tuple => tuple.repoPath == repositoryPath);
            }
        }

        /// <summary>Register a tab ID as belonging to a specific plugin</summary>
        public void RegisterTabToPlugin(string tabId, string pluginId)
        {
            lock (_lock)
            {
                _tabToPluginMap[tabId] = pluginId;
            }
        }

        /// <summary>Unregister a tab ID from a plugin</summary>
        public void UnregisterTabFromPlugin(string tabId, string pluginId)
        {
            lock (_lock)
            {
                if (_tabToPluginMap.TryGetValue(tabId, out var existing) && existing == pluginId)
                    _tabToPluginMap.Remove(tabId);
            }
        }

        /// <summary>Get all tab IDs registered for a specific plugin</summary>
        public HashSet<string> GetTabIdsForPlugin(string pluginId)
        {
            lock (_lock)
            {
                var result = new HashSet<string>();
                foreach (var kv in _tabToPluginMap)
                {
                    if (kv.Value == pluginId)
                        result.Add(kv.Key);
                }
                return result;
            }
        }

        private void RaisePluginStateChanged(string pluginId)
        {
            Action<string> handler;
            lock (_eventLock)
            {
                handler = _pluginStateChanged;
            }
            handler?.Invoke(pluginId);
        }

        private readonly object _lock = new();
        private readonly object _eventLock = new();
        private readonly List<IPluginManifest> _builtInManifests = new();
        private Action<string> _pluginStateChanged;
        private readonly HashSet<(string pluginId, string repoPath)> _activatedPlugins = new();
        private readonly Dictionary<string, string> _tabToPluginMap = new();
    }
}