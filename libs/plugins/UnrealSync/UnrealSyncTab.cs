using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Avalonia;

using UGSGit.PluginAbstractions;
using UGSGit.Plugins.UnrealSync.ViewModels;
using UGSGit.Plugins.UnrealSync.Views;

namespace UGSGit.Plugins.UnrealSync;

public class UnrealSyncTab : IRepositoryTab
{
    private readonly PluginContext _context;
    private readonly UnrealSyncTabViewModel _viewModel;
    private readonly UnrealSyncTabView _bodyView;
    private readonly StatusPanelView _toolbarView;

    public string Title => "UnrealSync";
    public object Icon => Application.Current?.Resources["Icons.UnrealSync"]!;
    public bool IsClosable => true;

    public string TabId { get; }

    public int SortOrder => 100;

    public object ToolbarContent => _toolbarView;

    public object BodyContent => _bodyView;

    public UnrealSyncTab(PluginContext context)
    {
        _context = context;
        var repoPath = context.RepositoryPath;
        TabId = $"unrealsync-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(repoPath)))[..8].ToLowerInvariant()}";

        var syncService = context.GetRequiredService<IGitSyncService>();
        _viewModel = new UnrealSyncTabViewModel(repoPath, syncService, context);

        // Create views with DataContext wired up to avoid raw VM type-name rendering
        _toolbarView = new StatusPanelView();
        _toolbarView.DataContext = _viewModel.StatusPanel;

        _bodyView = new UnrealSyncTabView();
        _bodyView.DataContext = _viewModel;
    }

    public void OnActivated()
    {
        _viewModel.RefreshAsync().ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    _context.GetService<IPluginLogger>()?.LogError(
                        $"UnrealSync OnActivated error: {t.Exception.Flatten().Message}",
                        t.Exception.Flatten());
                }
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }
    public void OnDeactivated() { }
    public void Dispose() => _viewModel.Dispose();
}
