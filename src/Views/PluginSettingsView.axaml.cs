using Avalonia.Controls;
using Avalonia.Interactivity;

using UGSGit.ViewModels;

namespace UGSGit.Views
{
    public partial class PluginSettingsView : UserControl
    {
        public PluginSettingsView()
        {
            InitializeComponent();
        }

        private void OnOpenPluginsFolder(object sender, RoutedEventArgs e)
        {
            if (DataContext is PluginSettingsViewModel vm)
                vm.OpenPluginsFolder();
        }
    }
}