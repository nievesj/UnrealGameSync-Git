#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

using UGSGit.PluginAbstractions;
using UGSGit.Plugins.UnrealSync.Views;

namespace UGSGit.Plugins.UnrealSync.ViewModels;

/// <summary>
/// Main workspace ViewModel handling sync, build, editor launch, packaging, and publishing.
/// </summary>
public partial class FullWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly string _repoPath;
    private readonly string _enginePath;
    private readonly string _uprojectPath;
    private readonly UProjectMeta _meta;
    private UgsConfig _config;
    private readonly IGitSyncService _syncService;
    private readonly IBuildService _buildService;
    private readonly IEditorLauncher _editorLauncher;
    private readonly IConfigService _configService;
    private readonly IEngineInfoService _engineInfoService;
    private readonly PluginContext _context;
    private CancellationTokenSource? _buildCts;
    private Process? _editorProcess;
    private readonly System.Text.StringBuilder _logBuilder = new();
    private int _isBusyFlag;

    /// <summary>Static header label displayed in the status panel.</summary>
    [ObservableProperty]
    private string _branchText = "Unreal Game Sync for Git";

    /// <summary>SHA of the commit the workspace is currently synced to.</summary>
    [ObservableProperty]
    private string _commitText = "";

    /// <summary>Subject line of the current commit.</summary>
    [ObservableProperty]
    private string _commitSubject = "";

    /// <summary>Aggregated log output text displayed in the log panel.</summary>
    [ObservableProperty]
    private string _logOutput = "";

    /// <summary>Indicates whether a sync, build, or package operation is in progress.</summary>
    public bool IsBusy => _isBusyFlag != 0;

    /// <summary>Atomically acquires the busy flag and notifies binding. Returns true if acquired, false if already busy.</summary>
    private bool TrySetBusy()
    {
        if (Interlocked.CompareExchange(ref _isBusyFlag, 1, 0) == 0)
        {
            OnPropertyChanged(nameof(IsBusy));
            return true;
        }
        return false;
    }

    /// <summary>Clears the busy flag and notifies binding.</summary>
    private void ClearBusy()
    {
        Interlocked.Exchange(ref _isBusyFlag, 0);
        OnPropertyChanged(nameof(IsBusy));
    }

    /// <summary>Display name of the project derived from the .uproject filename.</summary>
    [ObservableProperty]
    private string _projectName = "";

    /// <summary>Number of modules declared in the .uproject file.</summary>
    [ObservableProperty]
    private int _moduleCount;

    /// <summary>Number of plugins declared in the .uproject file.</summary>
    [ObservableProperty]
    private int _pluginCount;

    /// <summary>Engine association string from the .uproject file.</summary>
    [ObservableProperty]
    private string _engineAssociation = "";

    /// <summary>Detected engine version string (e.g. 5.4).</summary>
    [ObservableProperty]
    private string _engineVersionText = "";

    /// <summary>Engine build configuration type (e.g. Shipping, DebugGame).</summary>
    [ObservableProperty]
    private string _engineBuildType = "";

    /// <summary>Full path to the detected engine root directory.</summary>
    [ObservableProperty]
    private string _enginePathText = "";

    // Phase 1b: Package & Publish
    /// <summary>Whether the Package button is enabled.</summary>
    [ObservableProperty]
    private bool _canPackage = true;

    /// <summary>Whether the Publish button is enabled (true after a successful package).</summary>
    [ObservableProperty]
    private bool _canPublish;

    /// <summary>Path to the last successfully created zip archive.</summary>
    [ObservableProperty]
    private string _lastZipPath = string.Empty;

    /// <summary>Channel name the last zip should be published to (Editor or Game).</summary>
    [ObservableProperty]
    private string _lastZipChannel = "Editor";

    /// <summary>Package progress as a value between 0.0 and 1.0.</summary>
    [ObservableProperty]
    private double _packageProgress;

    /// <summary>Publish progress as a value between 0.0 and 1.0.</summary>
    [ObservableProperty]
    private double _publishProgress;

    /// <summary>Status text displayed during or after a publish operation.</summary>
    [ObservableProperty]
    private string _publishStatusText = "";

    /// <summary>Status text for the editor binary build deployment.</summary>
    [ObservableProperty]
    private string _editorBuildStatusText = "";

    /// <summary>Whether the deployed editor binary matches the current commit.</summary>
    [ObservableProperty]
    private bool _editorBuildIsCurrent;

    /// <summary>Whether a newer editor binary build is available on the network.</summary>
    [ObservableProperty]
    private bool _editorBuildAvailable;

    /// <summary>Observable collection of configured build target steps.</summary>
    public ObservableCollection<UgsBuildStep> BuildTargets { get; } = new();

    /// <summary>Observable collection of available package profiles.</summary>
    public ObservableCollection<UgsPackageProfile> PackageProfiles { get; } = new();

    /// <summary>
    /// Initializes a new instance of the FullWorkspaceViewModel with the specified repository context,
    /// engine path, project metadata, and service dependencies.
    /// </summary>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    /// <param name="enginePath">Absolute path to the Unreal Engine root directory.</param>
    /// <param name="meta">Parsed metadata from the .uproject file, including modules and plugins.</param>
    /// <param name="syncService">Service for Git sync operations (fetch, pull, status).</param>
    /// <param name="uprojectPath">Absolute path to the .uproject file.</param>
    /// <param name="buildService">Service for executing build targets.</param>
    /// <param name="editorLauncher">Service for launching the Unreal Editor process.</param>
    /// <param name="configService">Service for loading and saving UnrealSync config files.</param>
    /// <param name="engineInfoService">Service for detecting engine version and build type.</param>
    /// <param name="context">Plugin context providing access to additional host services.</param>
    public FullWorkspaceViewModel(
        string repoPath,
        string enginePath,
        UProjectMeta meta,
        IGitSyncService syncService,
        string uprojectPath,
        IBuildService buildService,
        IEditorLauncher editorLauncher,
        IConfigService configService,
        IEngineInfoService engineInfoService,
        PluginContext context)
    {
        _repoPath = repoPath;
        _enginePath = enginePath;
        _uprojectPath = uprojectPath;
        _meta = meta;
        _config = configService.LoadConfig(repoPath);
        _syncService = syncService;
        _buildService = buildService;
        _editorLauncher = editorLauncher;
        _configService = configService;
        _engineInfoService = engineInfoService;
        _context = context;

        ProjectName = System.IO.Path.GetFileNameWithoutExtension(uprojectPath);
        ModuleCount = meta.Modules?.Count ?? 0;
        PluginCount = meta.Plugins?.Count ?? 0;
        EngineAssociation = meta.EngineAssociation ?? "";

        // Engine info (Phase 1b)
        var engineInfo = _engineInfoService.Detect(enginePath);
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
        var replacement = new CancellationTokenSource();
        var old = Interlocked.Exchange(ref _buildCts, replacement);
        if (old != null)
        {
            try { if (!old.IsCancellationRequested) old.Cancel(); } catch { }
            old.Dispose();
        }
    }

    /// <summary>Syncs working tree to latest commit on current branch.</summary>
    [RelayCommand]
    private async Task SyncAsync()
    {
        if (!TrySetBusy()) return;

        try
        {
            var progress = new Progress<string>(AppendLog);
            var result = await _syncService.SyncToLatestAsync(
                BranchText, progress, CancellationToken.None).ConfigureAwait(true);

            AppendLog($"\n{result.Message}");

            if (result.Status == SyncStatus.Success && !string.IsNullOrEmpty(result.CommitSha))
            {
                CommitText = result.CommitSha;

                // Non-blocking binary deploy if network is configured
                if (!string.IsNullOrEmpty(_config.NetworkBase))
                {
                    try
                    {
                        await DeployEditorAsync();
                    }
                    catch (System.Exception ex)
                    {
                        AppendLog($"\nPost-sync deploy error: {ex.Message}");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            AppendLog($"\nSync error: {ex.Message}");
        }
        finally
        {
            ClearBusy();
        }
    }

    /// <summary>Runs all configured build targets.</summary>
    [RelayCommand]
    private async Task BuildAsync()
    {
        if (!TrySetBusy()) return;

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
                    new List<UgsBuildStep>(BuildTargets), progress, _buildCts!.Token, archiveDir).ConfigureAwait(true);
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
            ClearBusy();
        }
    }

    /// <summary>Cancels the active build operation.</summary>
    [RelayCommand]
    private void CancelBuild() => _buildCts?.Cancel();

    /// <summary>Launches the Unreal Editor with the current project.</summary>
    [RelayCommand]
    private void Launch()
    {
        try
        {
            _editorProcess = _editorLauncher.Launch(_uprojectPath);
            if (_editorProcess != null)
                AppendLog("\nEditor launched.");
        }
        catch (System.Exception ex)
        {
            AppendLog($"\nLaunch error: {ex.Message}");
        }
    }

    /// <summary>Packages the project using the selected profile.</summary>
    [RelayCommand]
    private async Task PackageAsync(UgsPackageProfile profile)
    {
        if (!TrySetBusy()) return;
        CanPublish = false;

        try
        {
            ResetCancellationToken();
            var progress = new Progress<string>(AppendLog);
            var buildGraphFactory = _context.GetService<IBuildGraphServiceFactory>()!;
            var buildGraph = buildGraphFactory.Create(_enginePath, _config);

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
                progress, _buildCts!.Token).ConfigureAwait(true);

            AppendLog($"\nStage {stageResult.Status}: {stageResult.Message}");

            // Guard: if stage failed, do NOT proceed to zip (fixes C-2)
            if (stageResult.Status != BuildStatus.Success || string.IsNullOrEmpty(stageResult.StagingDirectory))
                return;

            // Create zip (fixes C-1: uses structured StagingDirectory field)
            var zipResult = await buildGraph.CreateZipAsync(
                stageResult.StagingDirectory, zipPath,
                _config.Archive?.ExcludePdb ?? true,
                progress, _buildCts!.Token).ConfigureAwait(true);

            AppendLog($"\nZip created: {zipResult}");
            LastZipPath = zipResult;
            LastZipChannel = profile.EditorTarget.EndsWith("Editor", StringComparison.OrdinalIgnoreCase)
                ? _config.EditorChannel
                : _config.GameChannel;
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
            ClearBusy();
        }
    }

    /// <summary>Publishes the last built zip to the configured network location.</summary>
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

            var publishChannel = LastZipChannel;
            var atomic = _config.Publish?.Atomic ?? true;

            var progress = new Progress<PublishProgress>(p =>
            {
                PublishProgress = p.TotalBytes > 0 ? (double)p.BytesCopied / p.TotalBytes : 0;
                PublishStatusText = $"Publishing... {PublishProgress:P0}";
            });

            var service = _context.GetService<IPublishService>()!;
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

    /// <summary>Deploys the precompiled editor binary for the current commit.</summary>
    [RelayCommand]
    private async Task DeployEditorAsync()
    {
        if (!TrySetBusy()) return;

        try
        {
            var deployService = _context.GetService<IDeployService>()!;
            var shortSha = CommitText?.Length >= 9 ? CommitText[..9] : CommitText;
            var progress = new Progress<string>(AppendLog);

            if (string.IsNullOrEmpty(shortSha))
            {
                AppendLog("No commit to deploy.");
                return;
            }

            // Check if this SHA is already deployed
            var localState = _configService.LoadLocalState(_repoPath);
            if (string.Equals(localState.LastDeployedArchiveSha, shortSha, StringComparison.OrdinalIgnoreCase))
            {
                AppendLog($"Editor build {shortSha} is already deployed.");
                return;
            }

            var binaryName = !string.IsNullOrEmpty(_config.BinaryName)
                ? _config.BinaryName
                : ProjectName;

            var result = await deployService.DeployAsync(
                _repoPath, _config.NetworkBase, _config.EditorChannel,
                binaryName, shortSha, progress, CancellationToken.None).ConfigureAwait(true);

            AppendLog(result.Message);

            if (result.Status == DeployStatus.Success)
            {
                await RefreshEditorBuildStatusAsync();
            }
        }
        catch (System.Exception ex)
        {
            AppendLog($"\nDeploy error: {ex.Message}");
        }
        finally
        {
            ClearBusy();
        }
    }

    /// <summary>Refreshes the editor build status fields from local state and network.</summary>
    private async Task RefreshEditorBuildStatusAsync()
    {
        try
        {
            var localState = _configService.LoadLocalState(_repoPath);
            var shortSha = CommitText?.Length >= 9 ? CommitText[..9] : CommitText;
            var deployedSha = localState.LastDeployedArchiveSha;

            if (string.IsNullOrEmpty(deployedSha))
            {
                EditorBuildStatusText = "none";
                EditorBuildIsCurrent = false;
            }
            else if (string.Equals(deployedSha, shortSha, StringComparison.OrdinalIgnoreCase))
            {
                EditorBuildStatusText = $"Editor build: {deployedSha} (current)";
                EditorBuildIsCurrent = true;
            }
            else
            {
                EditorBuildStatusText = $"Editor build: {deployedSha} (behind)";
                EditorBuildIsCurrent = false;
            }

            // Check if a newer build is available on the network
            EditorBuildAvailable = false;
            if (!string.IsNullOrEmpty(_config.NetworkBase) && !string.IsNullOrEmpty(shortSha))
            {
                var deployService = _context.GetService<IDeployService>(); // returns null if not registered
                if (deployService != null)
                {
                    var binaryName = !string.IsNullOrEmpty(_config.BinaryName)
                        ? _config.BinaryName
                        : ProjectName;
                    var found = await deployService.FindBuildForCommitAsync(
                        _config.NetworkBase, _config.EditorChannel, binaryName,
                        shortSha, CancellationToken.None).ConfigureAwait(true);
                    EditorBuildAvailable = found != null;
                }
            }
        }
        catch
        {
            // Status display is best-effort; silently ignore errors
        }
    }

    /// <summary>Opens the settings dialog for configuring engine paths, build targets, and publish options.</summary>
    [RelayCommand]
    private async Task OpenSettings()
    {
        var dialogVm = new SettingsDialogViewModel(_repoPath, _enginePath, _uprojectPath, _configService);
        var dialog = new Views.SettingsDialog
        {
            DataContext = dialogVm
        };

        // Wire Cancel → close dialog (fixes CANCEL button)
        void OnRequestClose() => dialog.Close();
        dialogVm.RequestClose += OnRequestClose;

        var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
            ? lifetime.MainWindow
            : null;

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }

        dialogVm.RequestClose -= OnRequestClose;
        ReloadConfig();
    }

    /// <summary>Reloads config from disk and refreshes build targets + package profiles.</summary>
    public void ReloadConfig()
    {
        _config = _configService.LoadConfig(_repoPath);

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

    /// <summary>
    /// Fetches the current branch name and commit SHA from the Git repository.
    /// Also refreshes the editor build deployment status.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the fetch operations.</param>
    /// <returns>A task that completes when both branch and commit have been retrieved.</returns>
    public async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            CommitText = await _syncService.GetCurrentCommitAsync(ct).ConfigureAwait(true);
            await RefreshEditorBuildStatusAsync();
        }
        catch { /* ignore */ }
    }

    /// <summary>Cancels active build and disposes the editor process.</summary>
    public void Dispose()
    {
        ResetCancellationToken();
        _editorProcess?.Dispose();
    }

    private string FormatZipName(string? template, UgsPackageProfile profile)
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
