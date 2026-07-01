using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Models;
using UGSGit.PluginAbstractions;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// Represents a single plugin row in the global plugin settings UI.
    /// </summary>
    public class PluginRow : ObservableObject
    {
        public IPluginManifest Manifest { get; init; }
        public string DllPath { get; init; }
        public bool IsLoaded { get; init; }
        public bool IsSkipped { get; init; }
        public string ErrorMessage { get; init; }

        public string PluginId => Manifest?.PluginId ?? "";
        public string DisplayName => Manifest?.DisplayName ?? Path.GetFileName(DllPath);
        public string Description => Manifest?.Description ?? "";
        public string Version => Manifest?.Version ?? "?";
        public string Author => Manifest?.Author ?? "";
        public bool IsGlobalByDefault => Manifest?.IsGlobalByDefault ?? false;
        public int DefaultSortOrder => Manifest?.DefaultSortOrder ?? 1000;

        /// <summary>Whether this plugin is enabled globally</summary>
        public bool IsGloballyEnabled
        {
            get => PluginRegistry.Instance.IsEnabledGlobally(PluginId);
            set
            {
                PluginRegistry.Instance.SetGlobalDefault(PluginId, value);
                OnPropertyChanged();
            }
        }

        /// <summary>Whether this row represents a failed load</summary>
        public bool HasError => !IsLoaded && !IsSkipped && !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>Display string for default mode</summary>
        public string DefaultModeText => IsGlobalByDefault ? "Default: Global" : "Default: Per-Project";
    }
}
