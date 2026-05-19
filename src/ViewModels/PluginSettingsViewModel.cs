using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

using UGSGit.Models;

namespace UGSGit.ViewModels
{
    public class PluginSettingsViewModel : ObservableObject
    {
        public AvaloniaList<PluginRow> Plugins { get; } = new();

        public PluginSettingsViewModel()
        {
            Reload();
        }

        /// <summary>
        /// Reloads plugins from the registry. Only built-in manifests are shown as
        /// "(built-in)"; external manifests come from DiscoveredPlugins directly.
        /// This avoids the duplicate-row bug (Issue #3).
        /// </summary>
        public void Reload()
        {
            Plugins.Clear();

            // Built-in plugins only
            foreach (var manifest in PluginRegistry.Instance.BuiltInManifests)
            {
                Plugins.Add(new PluginRow
                {
                    Manifest = manifest,
                    DllPath = "(built-in)",
                    IsLoaded = true,
                });
            }

            // External plugin results (success, skipped, failed)
            foreach (var result in PluginRegistry.Instance.DiscoveredPlugins)
            {
                Plugins.Add(new PluginRow
                {
                    Manifest = result.IsSuccess ? result.Manifest : null,
                    DllPath = result.DllPath,
                    IsLoaded = result.IsSuccess,
                    IsSkipped = result.IsSkipped,
                    ErrorMessage = result.ErrorMessage,
                });
            }
        }

        public void OpenPluginsFolder()
        {
            var pluginDir = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "plugins");
            if (!System.IO.Directory.Exists(pluginDir))
                System.IO.Directory.CreateDirectory(pluginDir);
            Native.OS.OpenInFileManager(pluginDir);
        }
    }
}
