using Avalonia.Interactivity;

using SourceGit.ViewModels;

namespace SourceGit.Views
{
    public partial class PerRepoPluginDialog : ChromelessWindow
    {
        public PerRepoPluginDialog()
        {
            InitializeComponent();
        }

        private void OnResetAll(object sender, RoutedEventArgs e)
        {
            if (DataContext is PerRepoPluginDialogViewModel vm)
                vm.ResetAllToDefaults();
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
