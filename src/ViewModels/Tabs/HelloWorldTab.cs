using Avalonia;

using SourceGit.Models;

namespace SourceGit.ViewModels.Tabs
{
    public class HelloWorldTab : IRepositoryTab
    {
        private readonly HelloWorldToolbarViewModel _toolbarVM = new();
        private readonly HelloWorldBodyViewModel _bodyVM = new();

        public string Title => "Hello World";
        public object Icon => Avalonia.Application.Current?.Resources["Icons.TabHelloWorld"];
        public bool IsClosable => true;
        public string TabId => "hello-world";
        public int SortOrder => 500;

        public object ToolbarContent => _toolbarVM;
        public object BodyContent => _bodyVM;

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