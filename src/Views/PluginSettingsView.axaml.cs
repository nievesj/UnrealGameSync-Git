using Avalonia.Controls;
using Avalonia.Interactivity;

using SourceGit.ViewModels;

namespace SourceGit.Views
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