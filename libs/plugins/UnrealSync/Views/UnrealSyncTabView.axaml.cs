using Avalonia.Controls;

namespace UGSGit.Plugins.UnrealSync.Views;

/// <summary>
/// Root view that hosts ContentControl with DataTemplates for sub-views.
/// DataContext is set by UnrealSyncTab.
/// </summary>
public partial class UnrealSyncTabView : UserControl
{
    /// <summary>
    /// Initializes the view and its AXAML-defined components.
    /// </summary>
    public UnrealSyncTabView()
    {
        InitializeComponent();
    }
}
