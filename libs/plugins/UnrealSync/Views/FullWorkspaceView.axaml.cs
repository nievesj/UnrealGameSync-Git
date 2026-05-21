using Avalonia.Controls;

namespace UGSGit.Plugins.UnrealSync.Views;

/// <summary>
/// Main workspace view with sync, build, launch, package, and publish actions.
/// DataContext is set by UnrealSyncTab.
/// </summary>
public partial class FullWorkspaceView : UserControl
{
    /// <summary>
    /// Initializes the view and its AXAML-defined components.
    /// </summary>
    public FullWorkspaceView()
    {
        InitializeComponent();
    }
}
