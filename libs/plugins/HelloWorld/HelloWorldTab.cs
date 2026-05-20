using Avalonia;
using UGSGit.PluginAbstractions;
using UGSGit.Plugins.HelloWorld.ViewModels;
using UGSGit.Plugins.HelloWorld.Views;

namespace UGSGit.Plugins.HelloWorld
{
    public class HelloWorldTab : IRepositoryTab
    {
        private readonly HelloWorldToolbarViewModel _toolbarVM;
        private readonly HelloWorldBodyViewModel _bodyVM;

        public string Title => "Hello World";
        public object Icon => Avalonia.Application.Current?.Resources["Icons.TabHelloWorld"];
        public bool IsClosable => true;
        public string TabId { get; }
        public int SortOrder => 500;

        public object ToolbarContent { get; private set; }
        public object BodyContent { get; private set; }

        /// <summary>
        /// Creates a HelloWorld tab with a per-repository-unique TabId.
        /// Fixes Issue #7: tab collision when opening multiple repositories.
        /// </summary>
        public HelloWorldTab(string repoPath)
        {
            TabId = $"hello-world-{repoPath.GetHashCode():x}";

            _bodyVM = new HelloWorldBodyViewModel();
            _toolbarVM = new HelloWorldToolbarViewModel();

            BodyContent = new HelloWorldBody { DataContext = _bodyVM };
            ToolbarContent = new HelloWorldToolbar { DataContext = _toolbarVM };

            _bodyVM.UpdateStatus("Ready");
        }

        public void OnActivated()
        {
            _bodyVM.UpdateStatus("Tab activated");
        }

        public void OnDeactivated()
        {
            _bodyVM.UpdateStatus("Tab deactivated");
        }

        public void Dispose() { }
    }
}
