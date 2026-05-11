using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SourceGit.Models;
using SourceGit.Services;

namespace SourceGit.ViewModels.Tabs.UnrealSync;

public partial class FullWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly string _repoPath;
    private readonly string _enginePath;
    private readonly UProjectMeta _meta;
    private UgsConfig _config;
    private readonly GitSyncService _syncService;
    private readonly BuildService _buildService;
    private readonly EditorLauncher _editorLauncher;
    private CancellationTokenSource _buildCts = null!;
    private Process _editorProcess = null!;
    private readonly System.Text.StringBuilder _logBuilder = new();

    [ObservableProperty]
    private string _branchText = "";

    [ObservableProperty]
    private string _commitText = "";

    [ObservableProperty]
    private string _commitSubject = "";

    [ObservableProperty]
    private string _logOutput = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _projectName = "";

    [ObservableProperty]
    private int _moduleCount;

    [ObservableProperty]
    private int _pluginCount;

    [ObservableProperty]
    private string _engineAssociation = "";

    // Exposed for the view code-behind to create SettingsDialog
    public string RepoPath => _repoPath;
    [ObservableProperty]
    private string _engineVersionText = "";

    [ObservableProperty]
    private string _engineBuildType = "";

    [ObservableProperty]
    private string _enginePathText = "";

    // Phase 1b: Package & Publish
    [ObservableProperty]
    private bool _canPackage = true;

    [ObservableProperty]
    private bool _canPublish;

    [ObservableProperty]
    private string _lastZipPath = string.Empty;

    [ObservableProperty]
    private double _packageProgress;

    [ObservableProperty]
    private double _publishProgress;

    [ObservableProperty]
    private string _publishStatusText = "";

    public ObservableCollection<UgsBuildStep> BuildTargets { get; } = new();
    public ObservableCollection<UgsPackageProfile> PackageProfiles { get; } = new();

    public FullWorkspaceViewModel(string repoPath, string enginePath, UProjectMeta meta, GitSyncService syncService)
    {
        _repoPath = repoPath;
        _enginePath = enginePath;
        _meta = meta;
        _config = ConfigService.LoadConfig(repoPath);
        _syncService = syncService;
        _buildService = new BuildService(repoPath, enginePath);
        _editorLauncher = new EditorLauncher(enginePath);

        ProjectName = System.IO.Path.GetFileNameWithoutExtension(
            System.IO.Directory.GetFiles(repoPath, "*.uproject")[0]);
        ModuleCount = meta.Modules?.Count ?? 0;
        PluginCount = meta.Plugins?.Count ?? 0;
        EngineAssociation = meta.EngineAssociation ?? "";

        // Engine info (Phase 1b)
        var engineInfo = EngineInfoService.Detect(enginePath);
        EngineVersionText = engineInfo.Version;
        EngineBuildType = engineInfo.BuildType;
        EnginePathText = enginePath;

        // Load build targets from config
        if (_config.Engine?.BuildTargets != null)
        {
            foreach (var step in _config.Engine.BuildTargets)
                BuildTargets.Add(step);
        }

        // Load package profiles (Phase 1b)
        LoadPackageProfiles();
    }

    private void LoadPackageProfiles(UgsConfig? config = null)
    {
        config ??= _config;
        PackageProfiles.Clear();

        // Derive editor target name from project name
        var projectName = ProjectName;

        // Use config-driven profiles if available (fixes L-5)
        if (config.Archive?.Profiles is { Count: > 0 })
        {
            foreach (var profile in config.Archive.Profiles)
                PackageProfiles.Add(profile);
            return;
        }

        // Fall back to hardcoded defaults
        PackageProfiles.Add(new UgsPackageProfile(
            "editor-dev", $"Editor (Dev)", $"{projectName}Editor",
            "Win64", "Development", false));

        PackageProfiles.Add(new UgsPackageProfile(
            "game-ship", $"Game (Ship)", projectName,
            "Win64", "Shipping", false));

        PackageProfiles.Add(new UgsPackageProfile(
            "server-dev", $"Server (Dev)", $"{projectName}Server",
            "Linux", "Development", false));
    }

    private void ResetCancellationToken()
    {
        var old = Interlocked.Exchange(ref _buildCts!, null);
        if (old != null)
        {
            try { if (!old.IsCancellationRequested) old.Cancel(); } catch { }
            old.Dispose();
        }
        _buildCts = new CancellationTokenSource();
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var progress = new Progress<string>(AppendLog);
            var result = await _syncService.SyncToLatestAsync(
                BranchText, progress, CancellationToken.None).ConfigureAwait(true);

            AppendLog($"\n{result.Message}");

            if (result.Status == GitSyncService.SyncStatus.Success && !string.IsNullOrEmpty(result.CommitSha))
                CommitText = result.CommitSha;
        }
        catch (System.Exception ex)
        {
            AppendLog($"\nSync error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BuildAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            ResetCancellationToken();
            var progress = new Progress<string>(AppendLog);

            if (BuildTargets.Count > 0)
            {
                // Compute archiveDir from config's OutputDirectory (for {ArchiveDir} variable expansion)
                var archiveDir = Path.GetFullPath(Path.Combine(_repoPath,
                    _config.BuildDefaults?.OutputDirectory ?? "Saved/StagedBuilds"));

                var result = await _buildService.ExecuteAllAsync(
                    new List<UgsBuildStep>(BuildTargets), progress, _buildCts.Token, archiveDir).ConfigureAwait(true);
                AppendLog($"\nBuild {result.Status}: {result.Message}");
            }
            else
            {
                AppendLog("\nNo build targets configured.");
            }
        }
        catch (System.Exception ex)
        {
            AppendLog($"\nBuild error: {ex.Message}");
        }
        finally
        {
            _buildCts = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelBuild() => _buildCts?.Cancel();

    [RelayCommand]
    private void Launch()
    {
        try
        {
            var uprojectFiles = System.IO.Directory.GetFiles(_repoPath, "*.uproject");
            if (uprojectFiles.Length == 0) { AppendLog("\nNo .uproject found."); return; }

            _editorProcess = _editorLauncher.Launch(uprojectFiles[0]);
            if (_editorProcess != null)
                AppendLog("\nEditor launched.");
        }
        catch (System.Exception ex)
        {
            AppendLog($"\nLaunch error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PackageAsync(UgsPackageProfile profile)
    {
        if (IsBusy) return;
        IsBusy = true;
        CanPublish = false;

        try
        {
            ResetCancellationToken();
            var progress = new Progress<string>(AppendLog);
            var buildGraph = new BuildGraphService(_enginePath, _repoPath, _config);

            // Determine zip output path
            var zipName = FormatZipName(_config.Archive?.ZipNaming, profile);
            var stagingBase = _config.BuildDefaults?.OutputDirectory ?? "Saved/StagedBuilds";
            var zipPath = Path.GetFullPath(Path.Combine(_repoPath, stagingBase, zipName));

            // Stage via BuildGraph
            var stageResult = await buildGraph.StageAsync(
                profile.EditorTarget,
                profile.Platform,
                profile.Configuration,
                profile.IncludePdb,
                progress, _buildCts.Token).ConfigureAwait(true);

            AppendLog($"\nStage {stageResult.Status}: {stageResult.Message}");

            // Guard: if stage failed, do NOT proceed to zip (fixes C-2)
            if (stageResult.Status != BuildStatus.Success || string.IsNullOrEmpty(stageResult.StagingDirectory))
                return;

            // Create zip (fixes C-1: uses structured StagingDirectory field)
            var zipResult = await buildGraph.CreateZipAsync(
                stageResult.StagingDirectory, zipPath,
                _config.Archive?.ExcludePdb ?? true,
                progress, _buildCts.Token).ConfigureAwait(true);

            AppendLog($"\nZip created: {zipResult}");
            LastZipPath = zipResult;
            CanPublish = true;
        }
        catch (OperationCanceledException)
        {
            AppendLog("\nPackage cancelled.");
        }
        catch (System.Exception ex)
        {
            AppendLog($"\nPackage error: {ex.Message}");
        }
        finally
        {
            _buildCts = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PublishAsync()
    {
        if (!CanPublish || string.IsNullOrEmpty(LastZipPath)) return;

        try
        {
            if (string.IsNullOrEmpty(_config.NetworkBase))
            {
                AppendLog("\nPublish not configured. Open Settings to set network base URL.");
                return;
            }

            var publishChannel = _config.Publish?.Channel ?? "Editor";
            var atomic = _config.Publish?.Atomic ?? true;

            var progress = new Progress<PublishProgress>(p =>
            {
                PublishProgress = p.TotalBytes > 0 ? (double)p.BytesCopied / p.TotalBytes : 0;
                PublishStatusText = $"Publishing... {PublishProgress:P0}";
            });

            var service = new PublishService();
            var result = await service.PublishZipAsync(
                LastZipPath,
                _config.NetworkBase,
                publishChannel,
                atomic,
                progress, CancellationToken.None).ConfigureAwait(true);

            PublishStatusText = result.Status == PublishStatus.Success
                ? $"Published to {_config.NetworkBase}"
                : result.Message;
            AppendLog($"\n{result.Message}");
        }
        catch (System.Exception ex)
        {
            AppendLog($"\nPublish error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // Intentionally no-op so the view-side Click="OpenSettings" handler runs.
        // The Avalonia code-behind handles dialog creation and ShowDialogAsync.
    }

    /// <summary>
    /// Reload config and refresh build targets/package profiles after settings change.
    /// Called from the view code-behind after the Settings dialog closes.
    /// </summary>
    public void ReloadConfig()
    {
        _config = ConfigService.LoadConfig(_repoPath);

        // Refresh build targets
        BuildTargets.Clear();
        if (_config.Engine?.BuildTargets != null)
        {
            foreach (var step in _config.Engine.BuildTargets)
                BuildTargets.Add(step);
        }

        // Refresh package profiles
        LoadPackageProfiles();
    }

    public async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            BranchText = await _syncService.GetCurrentBranchAsync(ct).ConfigureAwait(true);
            CommitText = await _syncService.GetCurrentCommitAsync(ct).ConfigureAwait(true);
        }
        catch { /* ignore */ }
    }

    public void Dispose()
    {
        ResetCancellationToken();
    }

    private string FormatZipName(string template, UgsPackageProfile profile)
    {
        var shortSha = CommitText?.Length >= 7 ? CommitText[..7] : "unknown";
        return (template ?? "{target}-{platform}-{config}-{shortSha}.zip")
            .Replace("{branch}", BranchText ?? "unknown")
            .Replace("{target}", profile.EditorTarget)
            .Replace("{platform}", profile.Platform)
            .Replace("{config}", profile.Configuration)
            .Replace("{shortSha}", shortSha)
            .Replace("{timestamp}", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); // fixes L-1
    }

    private void AppendLog(string line)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _logBuilder.Append(line);
            _logBuilder.Append(System.Environment.NewLine);

            // Cap at 10000 chars to avoid unbounded growth
            const int maxLen = 10000;
            if (_logBuilder.Length > maxLen)
            {
                _logBuilder.Remove(0, _logBuilder.Length - maxLen);
            }

            LogOutput = _logBuilder.ToString();
        });
    }
}