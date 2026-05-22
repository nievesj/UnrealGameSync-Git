using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync.ViewModels;

/// <summary>
/// Mode indicating which sub-view to display in the tab body.
/// </summary>
public enum SyncTabMode
{
    /// <summary>
    /// Scanning the repository for a .uproject file.
    /// </summary>
    Detecting,

    /// <summary>
    /// Full workspace view with sync, build, and launch controls.
    /// </summary>
    FullWorkspace,

    /// <summary>
    /// Error view shown when the UE engine cannot be detected.
    /// </summary>
    EngineNotFound
}

/// <summary>
/// Orchestrates tab lifecycle, delegates to sub-viewmodels.
/// </summary>
public class UnrealSyncTabViewModel : ObservableObject
{
    private readonly PluginContext _context;
    private readonly string _repoPath;
    private readonly IGitSyncService _syncService;
    private SyncTabMode _mode = SyncTabMode.Detecting;
    private object _currentBody = null!;

    /// <summary>
    /// Read-only reference to the status panel ViewModel.
    /// </summary>
    public StatusPanelViewModel StatusPanel { get; }

    /// <summary>
    /// Currently active sub-view (select project / full workspace / engine not found).
    /// </summary>
    public object CurrentBody
    {
        get => _currentBody;
        private set
        {
            if (SetProperty(ref _currentBody, value))
                OnPropertyChanged(nameof(IsDetecting));
        }
    }

    /// <summary>
    /// True when the tab is scanning for a .uproject file and no sub-view has been assigned yet.
    /// </summary>
    public bool IsDetecting => Mode == SyncTabMode.Detecting && CurrentBody == null;
    public SyncTabMode Mode
    {
        get => _mode;
        private set
        {
            if (SetProperty(ref _mode, value))
                OnPropertyChanged(nameof(IsDetecting));
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="UnrealSyncTabViewModel"/>.
    /// </summary>
    /// <param name="repoPath">Absolute path to the repository root.</param>
    /// <param name="syncService">Git sync service for branch and commit queries.</param>
    /// <param name="context">Plugin context providing service resolution.</param>
    public UnrealSyncTabViewModel(string repoPath, IGitSyncService syncService, PluginContext context)
    {
        _repoPath = repoPath;
        _syncService = syncService;
        _context = context;
        StatusPanel = new StatusPanelViewModel(repoPath, _syncService);
    }

    /// <summary>
    /// Detects project file and transitions to the appropriate sub-view.
    /// </summary>
    /// <returns>A task that completes when project detection finishes.</returns>
    public async Task RefreshAsync()
    {
        Mode = SyncTabMode.Detecting;

        await Task.Run(() =>
        {
            // 1. Check if a project file is configured
            var configService = _context.GetRequiredService<IConfigService>();
            var config = configService.LoadConfig(_repoPath);
            var configuredProject = config.Engine?.ProjectFile;
            string? uprojectPath = null;

            if (!string.IsNullOrEmpty(configuredProject))
            {
                // Resolve relative paths against repo root
                var fullPath = System.IO.Path.GetFullPath(
                    System.IO.Path.IsPathRooted(configuredProject) ? configuredProject : System.IO.Path.Combine(_repoPath, configuredProject));
                if (System.IO.File.Exists(fullPath))
                    uprojectPath = fullPath;
            }

            // 2. If no configured project, scan repo recursively
            if (uprojectPath == null)
            {
                var uprojectFiles = System.IO.Directory.GetFiles(_repoPath, "*.uproject", System.IO.SearchOption.AllDirectories);
                if (uprojectFiles.Length == 1)
                    uprojectPath = uprojectFiles[0];
                else
                {
                    // 0 or multiple found — show selection UI
                    Dispatcher.UIThread.Post(() =>
                    {
                        Mode = SyncTabMode.Detecting;
                        CurrentBody = new SelectUProjectViewModel(_repoPath, uprojectFiles, OnUProjectSelected);
                    });
                    return;
                }
            }

            // 3. Proceed with the selected project
            ProceedWithProject(uprojectPath, configService);
        });
    }

    private void OnUProjectSelected(string uprojectPath)
    {
        var configService = _context.GetRequiredService<IConfigService>();

        // Persist the selection to config
        var config = configService.LoadConfig(_repoPath);
        var relativePath = System.IO.Path.IsPathRooted(uprojectPath)
            ? System.IO.Path.GetRelativePath(_repoPath, uprojectPath)
            : uprojectPath;
        config = config with
        {
            Engine = config.Engine with { ProjectFile = relativePath }
        };
        configService.SaveConfig(_repoPath, config);

        // Proceed
        ProceedWithProject(uprojectPath, configService);
    }

    private void ProceedWithProject(string uprojectPath, IConfigService configService)
    {
        var json = System.IO.File.ReadAllText(uprojectPath);
        var meta = UProjectMeta.ParseTolerant(json);

        var engineDetector = _context.GetService<IEngineDetector>()!;
        var enginePath = engineDetector.Detect(meta, System.IO.Path.GetDirectoryName(uprojectPath)!);

        Dispatcher.UIThread.Post(() =>
        {
            if (!string.IsNullOrEmpty(enginePath))
            {
                var buildServiceFactory = _context.GetService<IBuildServiceFactory>()!;
                var buildService = buildServiceFactory.Create(enginePath, uprojectPath);

                var editorLauncherFactory = _context.GetService<IEditorLauncherFactory>()!;
                var editorLauncher = editorLauncherFactory.Create(enginePath);

                var engineInfoService = _context.GetService<IEngineInfoService>()!;

                Mode = SyncTabMode.FullWorkspace;
                var bodyVm = new FullWorkspaceViewModel(
                    _repoPath, enginePath, meta, _syncService, uprojectPath,
                    buildService, editorLauncher, configService, engineInfoService, _context);
                CurrentBody = bodyVm;
            }
            else
            {
                Mode = SyncTabMode.EngineNotFound;
                CurrentBody = new EngineNotFoundViewModel(meta);
            }
        });
    }

    /// <summary>
    /// Cleans up CurrentBody if it implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (CurrentBody is System.IDisposable d)
            d.Dispose();
    }
}
