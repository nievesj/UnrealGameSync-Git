using Avalonia.Controls;

namespace UGSGit.Plugins.UnrealSync.Views;

/// <summary>
/// Toolbar/status bar view that displays branch and commit info.
/// DataContext is set by UnrealSyncTab.
/// </summary>
public partial class StatusPanelView : UserControl
{
    /// <summary>
    /// Initializes the view and its AXAML-defined components.
    /// </summary>
    public StatusPanelView()
    {
        InitializeComponent();
    }
}
