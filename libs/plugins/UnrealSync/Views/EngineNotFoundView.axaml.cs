using Avalonia.Controls;

namespace UGSGit.Plugins.UnrealSync.Views;

/// <summary>
/// Error view shown when the UE engine cannot be detected.
/// DataContext is set by UnrealSyncTab.
/// </summary>
public partial class EngineNotFoundView : UserControl
{
    /// <summary>
    /// Initializes the view and its AXAML-defined components.
    /// </summary>
    public EngineNotFoundView()
    {
        InitializeComponent();
    }
}
