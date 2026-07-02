using Avalonia;
using Avalonia.Media;

using UGSGit.Models;
using UGSGit.PluginAbstractions;

namespace UGSGit.ViewModels.Tabs
{
    public class RepositoryTab : IRepositoryTab
    {
        private readonly Repository _repo;
        private readonly RepositoryNode _node;

        public RepositoryTab(Repository repo, RepositoryNode node)
        {
            _repo = repo;
            _node = node;
        }

        public string Title => _node.Name;
        public object Icon => Avalonia.Application.Current?.Resources["Icons.Repositories"];
        public bool IsClosable => false;
        public string TabId => "repository";
        public int SortOrder => 0;

        public object ToolbarContent => _repo;
        public object BodyContent => _repo;

        public void OnActivated() { }
        public void OnDeactivated() { }
        public void Dispose() { }
    }
}