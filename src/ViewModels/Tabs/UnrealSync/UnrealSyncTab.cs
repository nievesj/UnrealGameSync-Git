using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Avalonia;

using SourceGit.Models;

namespace SourceGit.ViewModels.Tabs.UnrealSync;

public class UnrealSyncTab : IRepositoryTab
{
    private readonly UnrealSyncTabViewModel _viewModel;
    private readonly Views.Tabs.UnrealSync.UnrealSyncTabView _bodyView;
    private readonly Views.Tabs.UnrealSync.StatusPanelView _toolbarView;

    public string Title => "UnrealSync";
    public object Icon => Application.Current?.Resources["Icons.UnrealSync"]!;
    public bool IsClosable => true;

    public string TabId { get; }

    public int SortOrder => 100;

    public object ToolbarContent => _toolbarView;

    public object BodyContent => _bodyView;

    public UnrealSyncTab(string repoPath)
    {
        TabId = $"unrealsync-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(repoPath)))[..8].ToLowerInvariant()}";

        _viewModel = new UnrealSyncTabViewModel(repoPath);

        // Create views with DataContext wired up to avoid raw VM type-name rendering
        _toolbarView = new Views.Tabs.UnrealSync.StatusPanelView();
        _toolbarView.DataContext = _viewModel.StatusPanel;

        _bodyView = new Views.Tabs.UnrealSync.UnrealSyncTabView();
        _bodyView.DataContext = _viewModel;
    }

    public void OnActivated()
    {
        _viewModel.RefreshAsync().ContinueWith(
            t =>
            {
                if (t.Exception != null)
                    Native.OS.LogException(t.Exception.Flatten());
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }
    public void OnDeactivated() { }
    public void Dispose() => _viewModel.Dispose();
}
