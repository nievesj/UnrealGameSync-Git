using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Avalonia;

using UGSGit.PluginAbstractions;
using UGSGit.Plugins.UnrealSync.ViewModels;
using UGSGit.Plugins.UnrealSync.Views;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Composition root that wires Views and ViewModels together.
/// </summary>
public class UnrealSyncTab : IRepositoryTab
{
    private readonly PluginContext _context;
    private readonly UnrealSyncTabViewModel _viewModel;
    private readonly UnrealSyncTabView _bodyView;
    private readonly StatusPanelView _toolbarView;

    /// <summary>
    /// Tab title shown in the tab bar.
    /// </summary>
    public string Title => "UnrealSync";

    /// <summary>
    /// Tab icon resource key.
    /// </summary>
    public object Icon => Application.Current?.Resources["Icons.UnrealSync"]!;

    /// <summary>
    /// Whether the tab can be closed by the user (always true).
    /// </summary>
    public bool IsClosable => true;

    /// <summary>
    /// Unique identifier derived from repository path hash.
    /// </summary>
    public string TabId { get; }

    /// <summary>
    /// Sort position in the tab bar (100 = after Repository tab).
    /// </summary>
    public int SortOrder => 100;

    /// <summary>
    /// The status panel view.
    /// </summary>
    public object ToolbarContent => _toolbarView;

    /// <summary>
    /// The main content view (switches between select-project / workspace / error).
    /// </summary>
    public object BodyContent => _bodyView;

    /// <summary>
    /// Initializes the tab, creates the viewModel and wires DataContext on views.
    /// </summary>
    /// <param name="context">Plugin context providing repository path and service resolution.</param>
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

    /// <summary>
    /// Triggers async refresh of repository state.
    /// </summary>
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

    /// <summary>
    /// No-op (tab does not require cleanup on deactivation).
    /// </summary>
    public void OnDeactivated() { }

    /// <summary>
    /// Disposes the viewModel and cancels any pending operations.
    /// </summary>
    public void Dispose() => _viewModel.Dispose();
}
