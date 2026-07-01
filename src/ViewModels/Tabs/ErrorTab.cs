using System;

using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Models;
using UGSGit.PluginAbstractions;

namespace SourceGit.ViewModels.Tabs
{
    public class ErrorTab : IRepositoryTab
    {
        /// <summary>Full exception for diagnostics (Issue #12 — was unused)</summary>
        public Exception Error { get; }

        public string Title { get; }
        public object Icon => null;
        public object ToolbarContent => null;
        public object BodyContent { get; }
        public bool IsClosable => true;
        public string TabId { get; }
        public int SortOrder => int.MaxValue;

        public ErrorTab(string tabId, string title, Exception error)
        {
            Error = error;
            TabId = tabId + "-error";
            Title = title + " (Error)";
            BodyContent = new ErrorTabViewModel(TabId, Title, error.Message);
        }

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
