using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UGSGit.Views.Tabs.UnrealSync;

public partial class FullWorkspaceView : UserControl
{
    public FullWorkspaceView()
    {
        InitializeComponent();
    }

    private async void OpenSettings(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.Tabs.UnrealSync.FullWorkspaceViewModel vm)
            return;

        // Create settings dialog directly instead of relying on ShowDialogAsync magic
        var dialog = new SettingsDialog();

        // Find the owner window
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null)
        {
            dialog.DataContext = new ViewModels.Tabs.UnrealSync.SettingsDialogViewModel(vm.RepoPath, vm.EnginePathText, vm.UProjectPath);
            await dialog.ShowDialog(owner);

            // Refresh build targets and config after settings dialog closes
            vm.ReloadConfig();
        }

        e.Handled = true;
    }
}