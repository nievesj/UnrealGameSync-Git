using System.IO;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Models;
using SourceGit.Services;

namespace SourceGit.ViewModels.Tabs.UnrealSync;

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
        StatusPanel = new StatusPanelViewModel(repoPath);
    }

    public async Task RefreshAsync()
    {
        Mode = SyncTabMode.Detecting;

        await Task.Run(() =>
        {
            var uprojectFiles = Directory.GetFiles(_repoPath, "*.uproject");
            if (uprojectFiles.Length == 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Mode = SyncTabMode.NotUeProject;
                    CurrentBody = new NotUeProjectViewModel();
                });
                return;
            }

            var json = File.ReadAllText(uprojectFiles[0]);
            var meta = UProjectMeta.ParseTolerant(json);
            var enginePath = EngineDetector.Detect(meta, _repoPath);

            Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrEmpty(enginePath))
                {
                    Mode = SyncTabMode.FullWorkspace;
                    var bodyVm = new FullWorkspaceViewModel(_repoPath, enginePath, meta);
                    CurrentBody = bodyVm;
                }
                else
                {
                    Mode = SyncTabMode.EngineNotFound;
                    CurrentBody = new EngineNotFoundViewModel(meta);
                }
            });
        });
    }

    public void Dispose()
    {
        if (CurrentBody is System.IDisposable d)
            d.Dispose();
    }
}
