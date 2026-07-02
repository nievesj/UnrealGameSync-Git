using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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
    private readonly UnrealSyncCommitTypeAnnotator? _typeAnnotator;
    private readonly SyncEditorContributor? _menuContributor;
    private readonly LaunchEditorContributor? _launchContributor;
    private readonly BuildCommitContributor? _buildContributor;
    private readonly List<PackageCommitContributor> _packageContributors = new();
    private readonly PublishCommitContributor? _publishContributor;

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

        // Diagnostic logging
        var diagLogger = context.GetService<IPluginLogger>();
        diagLogger?.Log($"UnrealSyncTab created for repo: {repoPath}");

        // Register commit annotator for build availability badges
        // Only register if the repository contains a .uproject file —
        // build availability is an Unreal Engine concept and should not
        // appear in non-UE repositories.
        var hasUProject = HasUProjectFile(repoPath);
        diagLogger?.Log($"HasUProjectFile({repoPath}) = {hasUProject}");
        var deployService = context.GetService<IDeployService>();
        var configService = context.GetService<IConfigService>();
        var annotationProvider = context.GetService<ICommitAnnotationProvider>();
        diagLogger?.Log($"Services: deploy={deployService != null}, config={configService != null}, annotation={annotationProvider != null}");
        if (deployService != null && configService != null && hasUProject)
        {
            var logger = context.GetService<IPluginLogger>();
            _annotator = new UnrealSyncBuildAnnotator(deployService, configService, logger, context.RepositoryPath, context.RepositoryName);
        }

        // Register annotator with the host-level provider so badges appear in the commit graph
        if (_annotator != null && annotationProvider != null)
            annotationProvider.Register(_annotator);

        // Register commit type annotator for code/content badges
        // Only register if the repository contains a .uproject file —
        // Code/Content badges are an Unreal Engine concept and should not
        // appear in non-UE repositories.
        var gitFileQueryService = context.GetService<IGitFileQueryService>();
        if (gitFileQueryService != null && configService != null && hasUProject)
        {
            _typeAnnotator = new UnrealSyncCommitTypeAnnotator(gitFileQueryService, configService, context.GetService<IPluginLogger>(), context.RepositoryPath);
        }

        if (_typeAnnotator != null && annotationProvider != null)
            annotationProvider.Register(_typeAnnotator);

        // --- Register all commit menu contributors eagerly ---
        // These contribute to the graph context menu, which must be available even
        // before the UnrealSync tab has been activated.
        var menuContributorProvider = context.GetService<ICommitMenuContributorProvider>();

        // Register Sync Editor context menu contributor for commit graph right-click
        // Only register for UE projects — Sync Editor is an Unreal workflow feature.
        if (deployService != null && configService != null && hasUProject)
        {
            var logger = context.GetService<IPluginLogger>();
            _menuContributor = new SyncEditorContributor(deployService, configService, logger, context.RepositoryPath, context.RepositoryName);
        }

        if (_menuContributor != null && menuContributorProvider != null)
            menuContributorProvider.Register(_menuContributor);

        // Register Launch Editor context menu contributor for commit graph right-click
        // Only register for UE projects — Launch Editor is an Unreal workflow feature.
        var launcherFactory = context.GetService<IEditorLauncherFactory>();
        if (launcherFactory != null && configService != null && hasUProject)
        {
            var logger = context.GetService<IPluginLogger>();
            _launchContributor = new LaunchEditorContributor(launcherFactory, configService, logger, context.RepositoryPath);
        }

        if (_launchContributor != null && menuContributorProvider != null)
            menuContributorProvider.Register(_launchContributor);

        // Register Build and Publish contributors eagerly (they hide via IsVisible until VM is bound)
        if (menuContributorProvider != null && hasUProject)
        {
            _buildContributor = new BuildCommitContributor(repoPath);
            menuContributorProvider.Register(_buildContributor);

            _publishContributor = new PublishCommitContributor(repoPath);
            menuContributorProvider.Register(_publishContributor);
        }

        // Package profiles are dynamic — register them when the workspace VM is ready
        // Bind VM to Build/Publish on ready as well
        if (menuContributorProvider != null && hasUProject)
        {
            _viewModel.FullWorkspaceReady += OnFullWorkspaceReady;
            _viewModel.PackageProfilesRefreshed += OnPackageProfilesRefreshed;
        }
        // Eagerly initialize workspace state so context menu contributors
        // are bound to the VM even before the tab is first activated.
        _viewModel.RefreshAsync().ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    _context.GetService<IPluginLogger>()?.LogError(
                        $"UnrealSync constructor init error: {t.Exception.Flatten().Message}",
                        t.Exception.Flatten());
                }
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private void OnFullWorkspaceReady(FullWorkspaceViewModel vm)
    {
        var menuContributorProvider = _context.GetService<ICommitMenuContributorProvider>();
        if (menuContributorProvider == null) return;

        // Bind Build and Publish contributors to the concrete workspace VM
        _buildContributor?.SetViewModel(vm);
        _publishContributor?.SetViewModel(vm);

        // Unregister stale package contributors before registering new ones
        foreach (var old in _packageContributors)
            menuContributorProvider.Unregister(old);
        _packageContributors.Clear();

        // Register one Package contributor per profile (dynamic)
        foreach (var profile in vm.PackageProfiles)
        {
            var pkg = new PackageCommitContributor(vm, profile, _context.RepositoryPath);
            _packageContributors.Add(pkg);
            menuContributorProvider.Register(pkg);
        }
    }

    private void OnPackageProfilesRefreshed()
    {
        // Re-register package contributors with updated profiles
        if (_viewModel.CurrentBody is FullWorkspaceViewModel vm)
            OnFullWorkspaceReady(vm);
    }

    /// <summary>
    /// Triggers async refresh of repository state.
    /// Annotators and menu contributors remain registered across tab switches
    /// because they contribute to the Repository tab's commit graph and context menu,
    /// which are visible regardless of which tab is active.
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
    /// No-op: annotators and menu contributors remain registered while the repo is open
    /// so they continue appearing in the Repository tab's commit graph and context menu.
    /// Unregistration only happens in <see cref="Dispose"/> when the repo is closed.
    /// </summary>
    public void OnDeactivated() { }

    /// <summary>
    /// Checks whether the repository contains a .uproject file,
    /// indicating it is an Unreal Engine project.
    /// Only scans top-level and first-level subdirectories to avoid
    /// expensive deep scans on large repos.
    /// </summary>
    private static bool HasUProjectFile(string repoPath)
    {
        try
        {
            // Check top-level first (most common layout)
            if (Directory.GetFiles(repoPath, "*.uproject", SearchOption.TopDirectoryOnly).Length > 0)
                return true;

            // Check one level deep (e.g. Projects/MyGame/MyGame.uproject)
            foreach (var dir in Directory.GetDirectories(repoPath))
            {
                try
                {
                    if (Directory.GetFiles(dir, "*.uproject", SearchOption.TopDirectoryOnly).Length > 0)
                        return true;
                }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException) { }
            }

            return false;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    /// <summary>
    /// Disposes the viewModel, unregisters the annotator and menu contributors,
    /// and disposes the GitFileQueryService.
    /// </summary>
    public void Dispose()
    {
        _viewModel.FullWorkspaceReady -= OnFullWorkspaceReady;
        _viewModel.PackageProfilesRefreshed -= OnPackageProfilesRefreshed;

        if (_annotator != null)
            _context.GetService<ICommitAnnotationProvider>()?.Unregister(_annotator);

        if (_typeAnnotator != null)
            _context.GetService<ICommitAnnotationProvider>()?.Unregister(_typeAnnotator);

        if (_menuContributor != null)
            _context.GetService<ICommitMenuContributorProvider>()?.Unregister(_menuContributor);

        if (_launchContributor != null)
            _context.GetService<ICommitMenuContributorProvider>()?.Unregister(_launchContributor);

        var provider = _context.GetService<ICommitMenuContributorProvider>();
        if (_buildContributor != null) provider?.Unregister(_buildContributor);
        foreach (var pkg in _packageContributors) provider?.Unregister(pkg);
        if (_publishContributor != null) provider?.Unregister(_publishContributor);

        // GitFileQueryService no longer implements IDisposable (throttling moved to process-wide GitProcessLimiter)
        // No disposal needed — the service has no unmanaged resources.

        _viewModel.Dispose();
    }
}
