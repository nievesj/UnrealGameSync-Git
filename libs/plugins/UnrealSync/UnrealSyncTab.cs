using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Avalonia;

using UGSGit.Models;

namespace UGSGit.ViewModels.Tabs.UnrealSync;

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

    public UnrealSyncTab(PluginContext context)
    {
        var repoPath = context.RepositoryPath;
        TabId = $"unrealsync-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(repoPath)))[..8].ToLowerInvariant()}";

        var syncService = context.GetService<IGitSyncService>()!;
        _viewModel = new UnrealSyncTabViewModel(repoPath, syncService, context);

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
                {
                    // Exception logged to console; plugin cannot access Native.OS directly
                    System.Console.Error.WriteLine($"UnrealSync OnActivated error: {t.Exception.Flatten().Message}");
                }
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }
    public void OnDeactivated() { }
    public void Dispose() => _viewModel.Dispose();
}
