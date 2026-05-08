using System;

using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Models;

namespace SourceGit.ViewModels.Tabs
{
    public class ErrorTab : IRepositoryTab
    {
        private readonly Exception _error;

        public ErrorTab(string tabId, string title, Exception error)
        {
            TabId = tabId + "-error";
            Title = title + " (Error)";
            _error = error;
        }

        public string Title { get; }
        public object Icon => null;
        public object ToolbarContent => null;
        public object BodyContent => new ErrorTabViewModel(TabId, Title, _error.Message);
        public bool IsClosable => true;
        public string TabId { get; }
        public int SortOrder => int.MaxValue;

        public void OnActivated() { }
        public void OnDeactivated() { }
        public void Dispose() { }
    }

    public class ErrorTabViewModel : ObservableObject
    {
        public string TabId { get; }
        public string TabTitle { get; }
        public string ErrorMessage { get; }

        public ErrorTabViewModel(string tabId, string tabTitle, string errorMessage)
        {
            TabId = tabId;
            TabTitle = tabTitle;
            ErrorMessage = errorMessage;
        }
    }
}