using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using UGSGit.Models;
using UGSGit.PluginAbstractions;

namespace UGSGit.ViewModels
{
    public class RepositoryTabDescriptor : ObservableObject
    {
        private readonly IRepositoryTab _tab;
        private object _cachedToolbarContent;
        private object _cachedBodyContent;
        private bool _isDisposed;

        public IRepositoryTab Tab => _tab;
        public string TabId => _tab.TabId;
        public string Title => _tab.Title;
        public object Icon => _tab.Icon;
        public bool IsClosable => _tab.IsClosable;
        public int SortOrder => _tab.SortOrder;

        public object ToolbarContent
        {
            get
            {
                if (_cachedToolbarContent == null)
                    _cachedToolbarContent = _tab.ToolbarContent;
                return _cachedToolbarContent;
            }
        }

        public object BodyContent
        {
            get
            {
                if (_cachedBodyContent == null)
                    _cachedBodyContent = _tab.BodyContent;
                return _cachedBodyContent;
            }
        }

        public IRelayCommand CloseTabCommand { get; }

        public event EventHandler RequestClose;

        public RepositoryTabDescriptor(IRepositoryTab tab)
        {
            _tab = tab;
            CloseTabCommand = new RelayCommand(OnRequestClose, () => tab.IsClosable);
        }

        private void OnRequestClose()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        internal void NotifyActivated() => _tab.OnActivated();
        internal void NotifyDeactivated() => _tab.OnDeactivated();

        internal void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _tab.Dispose();
        }
    }
}