using Avalonia.Controls;

using UGSGit.Plugins.UnrealSync.ViewModels;

namespace UGSGit.Plugins.UnrealSync.Views;

/// <summary>
/// Settings dialog for configuring engine path, build targets, and publish options.
/// DataContext is set by FullWorkspaceViewModel.
/// </summary>
public partial class SettingsDialog : Window
{
    /// <summary>
    /// Initializes the dialog and its AXAML-defined components.
    /// </summary>
    public SettingsDialog()
    {
        InitializeComponent();
    }

}