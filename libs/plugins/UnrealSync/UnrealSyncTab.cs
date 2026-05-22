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
    private readonly UnrealSyncBuildAnnotator? _annotator;
    private readonly SyncEditorContributor? _menuContributor;

    /// <summary>
    /// Tab title shown in the tab bar.
    /// </summary>
    public string Title => "UnrealSync";

    /// <summary>
    /// Tab icon resource key.
    /// </summary>
    public object Icon => Application.Current?.Resources.TryGetValue("Icons.UnrealSync", out var icon) == true
        ? icon!
        : "🔧";

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

        // Register commit annotator for build availability badges
        var deployService = context.GetService<IDeployService>();
        var configService = context.GetService<IConfigService>();
        var annotationProvider = context.GetService<ICommitAnnotationProvider>();
        if (deployService != null && configService != null)
        {
            var projectName = Path.GetFileNameWithoutExtension(context.RepositoryPath);
            var logger = context.GetService<IPluginLogger>();
            _annotator = new UnrealSyncBuildAnnotator(deployService, configService, logger, context.RepositoryPath, projectName);
        }

        // Register annotator with the host-level provider so badges appear in the commit graph
        if (_annotator != null && annotationProvider != null)
            annotationProvider.Register(_annotator);

        // Register Sync Editor context menu contributor for commit graph right-click
        if (deployService != null && configService != null)
        {
            var projectName = Path.GetFileNameWithoutExtension(context.RepositoryPath);
            var logger = context.GetService<IPluginLogger>();
            _menuContributor = new SyncEditorContributor(deployService, configService, logger, context.RepositoryPath, projectName);
        }

        var menuContributorProvider = context.GetService<ICommitMenuContributorProvider>();
        if (_menuContributor != null && menuContributorProvider != null)
            menuContributorProvider.Register(_menuContributor);
    }

    /// <summary>
    /// Triggers async refresh of repository state.
    /// Annotator is registered in constructor via PluginContext.
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
    /// Unregisters the commit annotator and menu contributor when the tab is deactivated.
    /// </summary>
    public void OnDeactivated()
    {
        if (_annotator != null)
            _context.GetService<ICommitAnnotationProvider>()?.Unregister(_annotator);

        if (_menuContributor != null)
            _context.GetService<ICommitMenuContributorProvider>()?.Unregister(_menuContributor);
    }

    /// <summary>
    /// Disposes the viewModel, unregisters the annotator and menu contributor.
    /// </summary>
    public void Dispose()
    {
        if (_annotator != null)
            _context.GetService<ICommitAnnotationProvider>()?.Unregister(_annotator);

        if (_menuContributor != null)
            _context.GetService<ICommitMenuContributorProvider>()?.Unregister(_menuContributor);

        _viewModel.Dispose();
    }
}
