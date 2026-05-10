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
        public string TabId { get; }
        public int SortOrder => 500;

        public object ToolbarContent => _toolbarVM;
        public object BodyContent => _bodyVM;

        /// <summary>
        /// Creates a HelloWorld tab with a per-repository-unique TabId.
        /// Fixes Issue #7: tab collision when opening multiple repositories.
        /// </summary>
        public HelloWorldTab(string repoPath)
        {
            // TabId uses repoPath hash for per-repository uniqueness.
            // NOTE: GetHashCode() is used only for in-memory UI identification, never persisted.
            // Two different repo paths colliding in GetHashCode is astronomically unlikely in practice.
            TabId = $"hello-world-{repoPath.GetHashCode():x}";
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
