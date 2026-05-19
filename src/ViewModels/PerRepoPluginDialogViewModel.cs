using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Models;

namespace SourceGit.ViewModels
{
    public class PerRepoPluginDialogViewModel : ObservableObject
    {
        public enum OverrideState
        {
            Inherit,      // Use global default
            ForceEnable,  // Override to enabled
            ForceDisable  // Override to disabled
        }

        public AvaloniaList<PluginOverrideRow> Plugins { get; } = new();

        public string RepositoryName { get; }

        public PerRepoPluginDialogViewModel(string repoName, RepositoryUIStates uiStates)
        {
            RepositoryName = repoName;

            foreach (var manifest in PluginRegistry.Instance.AllManifests)
            {
                Plugins.Add(new PluginOverrideRow(manifest, uiStates));
            }
        }

        public void ResetAllToDefaults()
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.State != OverrideState.Inherit)
                    plugin.State = OverrideState.Inherit;
            }
        }
    }
}
