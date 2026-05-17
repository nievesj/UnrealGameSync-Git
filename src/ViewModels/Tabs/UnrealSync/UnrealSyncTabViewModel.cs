using System.IO;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using UGSGit.Models;
using UGSGit.Services;

namespace UGSGit.ViewModels.Tabs.UnrealSync;

public enum SyncTabMode
{
    Detecting,
    FullWorkspace,
    NotUeProject,
    EngineNotFound
}

public class UnrealSyncTabViewModel : ObservableObject
{
    private readonly string _repoPath;
    private readonly GitSyncService _syncService;
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

    public UnrealSyncTabViewModel(string repoPath)
    {
        _repoPath = repoPath;
        _syncService = new GitSyncService(repoPath);
        StatusPanel = new StatusPanelViewModel(repoPath, _syncService);
    }

    public async Task RefreshAsync()
    {
        Mode = SyncTabMode.Detecting;

        await Task.Run(() =>
        {
            // 1. Check if a project file is configured
            var config = ConfigService.LoadConfig(_repoPath);
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
                        Mode = SyncTabMode.NotUeProject;
                        CurrentBody = new SelectUProjectViewModel(_repoPath, uprojectFiles, OnUProjectSelected);
                    });
                    return;
                }
            }

            // 3. Proceed with the selected project
            ProceedWithProject(uprojectPath);
        });
    }

    private void OnUProjectSelected(string uprojectPath)
    {
        // Persist the selection to config
        var config = ConfigService.LoadConfig(_repoPath);
        var relativePath = System.IO.Path.IsPathRooted(uprojectPath)
            ? System.IO.Path.GetRelativePath(_repoPath, uprojectPath)
            : uprojectPath;
        config = config with
        {
            Engine = config.Engine with { ProjectFile = relativePath }
        };
        ConfigService.SaveConfig(_repoPath, config);

        // Proceed
        ProceedWithProject(uprojectPath);
    }

    private void ProceedWithProject(string uprojectPath)
    {
        var json = System.IO.File.ReadAllText(uprojectPath);
        var meta = UProjectMeta.ParseTolerant(json);
        var enginePath = EngineDetector.Detect(meta, System.IO.Path.GetDirectoryName(uprojectPath)!);

        Dispatcher.UIThread.Post(() =>
        {
            if (!string.IsNullOrEmpty(enginePath))
            {
                Mode = SyncTabMode.FullWorkspace;
                var bodyVm = new FullWorkspaceViewModel(_repoPath, enginePath, meta, _syncService, uprojectPath);
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
