using System;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Models;
using SourceGit.ViewModels.Tabs;

namespace SourceGit.ViewModels
{
    public class LauncherPage : ObservableObject
    {
        public RepositoryNode Node
        {
            get => _node;
            set => SetProperty(ref _node, value);
        }

        public object Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        public Models.DirtyState DirtyState
        {
            get => _dirtyState;
            private set => SetProperty(ref _dirtyState, value);
        }

        public Popup Popup
        {
            get => _popup;
            set => SetProperty(ref _popup, value);
        }

        public AvaloniaList<Models.Notification> Notifications
        {
            get;
            set;
        } = new AvaloniaList<Models.Notification>();

        public AvaloniaList<RepositoryTabDescriptor> Tabs { get; } = new();

        public RepositoryTabDescriptor SelectedTab
        {
            get => _selectedTab;
            set
            {
                var previous = _selectedTab;
                if (SetProperty(ref _selectedTab, value))
                {
                    OnPropertyChanged(nameof(ToolbarContent));
                    OnPropertyChanged(nameof(BodyContent));

                    if (previous != null)
                        previous.NotifyDeactivated();

                    if (value != null)
                    {
                        value.NotifyActivated();
                        PersistActiveTabId(value.TabId);
                    }
                }
            }
        }

        /// <summary>
        /// Toolbar content: uses SelectedTab's toolbar if available, otherwise falls back to Data (Welcome page).
        /// </summary>
        public object ToolbarContent => _selectedTab?.ToolbarContent ?? _data;

        /// <summary>
        /// Body content: uses SelectedTab's body if available, otherwise falls back to Data (Welcome page).
        /// </summary>
        public object BodyContent => _selectedTab?.BodyContent ?? _data;

        public bool IsTabBarVisible => Tabs.Count > 1;

        public LauncherPage()
        {
            _node = new RepositoryNode() { Id = Guid.NewGuid().ToString() };
            _data = Welcome.Instance;

            // New welcome page will clear the search filter before.
            Welcome.Instance.ClearSearchFilter();

            Tabs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsTabBarVisible));
        }

        public LauncherPage(RepositoryNode node, Repository repo)
        {
            _node = node;
            _data = repo;

            Tabs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsTabBarVisible));

            RegisterBuiltInTabs();
            RestoreActiveTab();
        }

        public void AddPluginTab(IRepositoryTab tab)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LauncherPage));

            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => AddPluginTab(tab));
                return;
            }

            // Duplicate TabId guard
            if (Tabs.Any(t => t.TabId == tab.TabId))
            {
                Native.OS.LogException(new InvalidOperationException($"Duplicate tab ID '{tab.TabId}' — ignoring"));
                return;
            }

            RepositoryTabDescriptor descriptor;
            try
            {
                // Test that content properties don't throw
                _ = tab.ToolbarContent;
                _ = tab.BodyContent;
                descriptor = new RepositoryTabDescriptor(tab);
            }
            catch (Exception ex)
            {
                Native.OS.LogException(new InvalidOperationException($"Plugin tab '{tab.TabId}' failed to initialize: {ex.Message}"));
                descriptor = CreateErrorTabDescriptor(tab.TabId, tab.Title, ex);
            }

            // Insert at correct position based on SortOrder
            var insertIndex = 0;
            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].SortOrder <= descriptor.SortOrder)
                    insertIndex = i + 1;
                else
                    break;
            }
            Tabs.Insert(insertIndex, descriptor);

            descriptor.RequestClose += OnTabRequestClose;
        }

        public void RemovePluginTab(IRepositoryTab tab)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => RemovePluginTab(tab));
                return;
            }

            var descriptor = Tabs.FirstOrDefault(t => t.Tab == tab);
            if (descriptor == null)
                return;
            if (!descriptor.IsClosable)
                return;

            descriptor.RequestClose -= OnTabRequestClose;

            // If removing the selected tab, switch to the previous one
            if (SelectedTab == descriptor)
            {
                var fallbackIndex = Math.Max(0, Tabs.IndexOf(descriptor) - 1);
                SelectedTab = Tabs[fallbackIndex];
            }

            Tabs.Remove(descriptor);
            descriptor.Dispose();
        }

        public void SelectTab(int index)
        {
            if (index >= 0 && index < Tabs.Count)
                SelectedTab = Tabs[index];
        }

        public void SelectTab(string tabId)
        {
            var tab = Tabs.FirstOrDefault(t => t.TabId == tabId);
            if (tab != null)
                SelectedTab = tab;
        }

        public void SelectNextTab(bool reverse = false)
        {
            if (Tabs.Count <= 1)
                return;

            var currentIndex = Tabs.IndexOf(SelectedTab);
            if (currentIndex < 0)
                return;

            int nextIndex;
            if (reverse)
            {
                nextIndex = currentIndex - 1;
                if (nextIndex < 0)
                    nextIndex = Tabs.Count - 1;
            }
            else
            {
                nextIndex = currentIndex + 1;
                if (nextIndex >= Tabs.Count)
                    nextIndex = 0;
            }

            SelectedTab = Tabs[nextIndex];
        }

        public void ClearNotifications()
        {
            Notifications.Clear();
        }

        public void ChangeDirtyState(Models.DirtyState flag, bool remove)
        {
            var state = _dirtyState;
            if (remove)
            {
                if (state.HasFlag(flag))
                    state -= flag;
            }
            else
            {
                state |= flag;
            }

            DirtyState = state;
        }

        public bool CanCreatePopup()
        {
            return _popup is not { InProgress: true };
        }

        public async Task ProcessPopupAsync()
        {
            if (_popup is { InProgress: false } dump)
            {
                if (!dump.Check())
                    return;

                dump.InProgress = true;

                try
                {
                    var finished = await dump.Sure();
                    if (finished)
                    {
                        dump.Cleanup();
                        Popup = null;
                    }
                }
                catch (Exception e)
                {
                    Native.OS.LogException(e);
                }

                dump.InProgress = false;
            }
        }

        public void CancelPopup()
        {
            if (_popup == null || _popup.InProgress)
                return;

            _popup?.Cleanup();
            Popup = null;
        }

        internal void Dispose()
        {
            _isDisposed = true;
            foreach (var descriptor in Tabs)
                descriptor.Dispose();
            Tabs.Clear();
        }

        private void RegisterBuiltInTabs()
        {
            var repo = _data as Repository;
            if (repo == null)
                return;

            var repoTab = new RepositoryTab(repo, _node);
            Tabs.Add(new RepositoryTabDescriptor(repoTab));

            // Register Hello World reference plugin
            Tabs.Add(new RepositoryTabDescriptor(new HelloWorldTab()));

            SelectedTab = Tabs[0];
        }

        private void RestoreActiveTab()
        {
            if (_data is Repository repo)
            {
                var savedTabId = repo.UIStates.ActiveTabId;
                if (!string.IsNullOrEmpty(savedTabId))
                {
                    var tab = Tabs.FirstOrDefault(t => t.TabId == savedTabId);
                    if (tab != null)
                        SelectedTab = tab;
                }
            }
            // If savedTabId invalid or tab not found, SelectedTab stays at Tabs[0] (Repository)
        }

        private void PersistActiveTabId(string tabId)
        {
            if (_data is Repository repo)
                repo.UIStates.ActiveTabId = tabId;
        }

        private void OnTabRequestClose(object sender, EventArgs e)
        {
            if (sender is RepositoryTabDescriptor descriptor)
                RemovePluginTab(descriptor.Tab);
        }

        private RepositoryTabDescriptor CreateErrorTabDescriptor(string tabId, string title, Exception error)
        {
            var errorTab = new ErrorTab(tabId, title, error);
            return new RepositoryTabDescriptor(errorTab);
        }

        private RepositoryNode _node = null;
        private object _data = null;
        private Models.DirtyState _dirtyState = Models.DirtyState.None;
        private Popup _popup = null;
        private RepositoryTabDescriptor _selectedTab = null;
        private bool _isDisposed = false;
    }
}
