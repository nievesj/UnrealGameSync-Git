using System;

namespace SourceGit.Models
{
    public interface IRepositoryTab : IDisposable
    {
        string Title { get; }
        object Icon { get; }
        object ToolbarContent { get; }
        object BodyContent { get; }
        bool IsClosable { get; }
        string TabId { get; }
        int SortOrder { get; }

        void OnActivated();
        void OnDeactivated();
    }
}