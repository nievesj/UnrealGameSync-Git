using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Models;
using UGSGit.PluginAbstractions;

namespace SourceGit.ViewModels
{
    public class PluginOverrideRow : ObservableObject
    {
        public IPluginManifest Manifest { get; init; }
        private readonly RepositoryUIStates _uiStates;

        public string PluginId => Manifest.PluginId;
        public string DisplayName => Manifest.DisplayName;
        public string Version => Manifest.Version;
        public string Author => Manifest.Author;
        public string Description => Manifest.Description;

        /// <summary>Whether this plugin is enabled globally (inherited default)</summary>
        public bool IsGlobalDefaultEnabled => PluginRegistry.Instance.IsEnabledGlobally(PluginId);

        /// <summary>Current override state for this plugin in this repository</summary>
        public PerRepoPluginDialogViewModel.OverrideState State
        {
            get
            {
                var overrideVal = PluginRegistry.Instance.GetPerRepositoryOverride(PluginId, _uiStates);
                if (overrideVal == true) return PerRepoPluginDialogViewModel.OverrideState.ForceEnable;
                if (overrideVal == false) return PerRepoPluginDialogViewModel.OverrideState.ForceDisable;
                return PerRepoPluginDialogViewModel.OverrideState.Inherit;
            }
            set
            {
                switch (value)
                {
                    case PerRepoPluginDialogViewModel.OverrideState.Inherit:
                        PluginRegistry.Instance.ResetForRepository(PluginId, _uiStates);
                        break;
                    case PerRepoPluginDialogViewModel.OverrideState.ForceEnable:
                        PluginRegistry.Instance.SetEnabledForRepository(PluginId, _uiStates, true);
                        break;
                    case PerRepoPluginDialogViewModel.OverrideState.ForceDisable:
                        PluginRegistry.Instance.SetEnabledForRepository(PluginId, _uiStates, false);
                        break;
                }
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(IsEnabledForRepo));
                OnPropertyChanged(nameof(StateDisplayText));
                OnPropertyChanged(nameof(StateIndex));
            }
        }

        /// <summary>Whether this plugin is currently enabled for this repo (effective value)</summary>
        public bool IsEnabledForRepo => PluginRegistry.Instance.IsEnabledForRepository(PluginId, _uiStates);

        /// <summary>Display text for the override state</summary>
        public string StateDisplayText => State switch
        {
            PerRepoPluginDialogViewModel.OverrideState.Inherit => IsGlobalDefaultEnabled ? "Inherit (Enabled)" : "Inherit (Disabled)",
            PerRepoPluginDialogViewModel.OverrideState.ForceEnable => "Enabled",
            PerRepoPluginDialogViewModel.OverrideState.ForceDisable => "Disabled",
            _ => "Inherit"
        };

        /// <summary>Index-based binding for ComboBox (0=Inherit, 1=ForceEnable, 2=ForceDisable)</summary>
        public int StateIndex
        {
            get => (int)State;
            set => State = (PerRepoPluginDialogViewModel.OverrideState)value;
        }

        public PluginOverrideRow(IPluginManifest manifest, RepositoryUIStates uiStates)
        {
            Manifest = manifest;
            _uiStates = uiStates;
        }
    }
}
