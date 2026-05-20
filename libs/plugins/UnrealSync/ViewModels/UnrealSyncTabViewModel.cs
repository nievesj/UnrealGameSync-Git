using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync.ViewModels;

public enum SyncTabMode
{
    Detecting,
    FullWorkspace,
    EngineNotFound
}

public class UnrealSyncTabViewModel : ObservableObject
{
    private readonly PluginContext _context;
    private readonly string _repoPath;
    private readonly IGitSyncService _syncService;
    private SyncTabMode _mode = SyncTabMode.Detecting;
    private object _currentBody = null!;

    public StatusPanelViewModel StatusPanel { get; }

    public object CurrentBody
    {
        get => _currentBody;
        private set => SetProperty(ref _currentBody, value);
    }

    public SyncTabMode Mode
    {
        get => _mode;
        private set => SetProperty(ref _mode, value);
    }

    public UnrealSyncTabViewModel(string repoPath, IGitSyncService syncService, PluginContext context)
    {
        _repoPath = repoPath;
        _syncService = syncService;
        _context = context;
        StatusPanel = new StatusPanelViewModel(repoPath, _syncService);
    }

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

    public void Dispose()
    {
        if (CurrentBody is System.IDisposable d)
            d.Dispose();
    }
}
