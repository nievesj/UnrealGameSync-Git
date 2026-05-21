using Avalonia.Controls;

namespace UGSGit.Plugins.UnrealSync.Views;

/// <summary>
/// Project selection view for browsing or picking .uproject files.
/// DataContext is set by UnrealSyncTab.
/// </summary>
public partial class SelectUProjectView : UserControl
{
    /// <summary>
    /// Initializes the view and its AXAML-defined components.
    /// </summary>
    public SelectUProjectView()
    {
        InitializeComponent();
    }
}
