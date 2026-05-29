using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Commit context menu contributor that adds a "Launch Editor" item.
/// Opens the Unreal Editor with the current project's .uproject file.
/// Fast operation (Process.Start) — no progress popup needed.
///
/// Editor availability is determined by checking the local filesystem for the
/// editor binary, following the same pattern as UnrealGameSync which checks
/// for .target receipt files in the project's Binaries directory.
/// </summary>
public class LaunchEditorContributor : ICommitMenuContributor
{
    private readonly IEditorLauncherFactory _launcherFactory;
    private readonly IConfigService _configService;
    private readonly IPluginLogger? _logger;
    private readonly string _repoPath;

    private string? _cachedEditorPath;
    private string? _cachedUprojectPath;
    private string? _cachedProjectName;
    private bool _configCached;

    /// <inheritdoc/>
    public string Header => "Launch Editor";

    /// <inheritdoc/>
    public string? IconResourceKey => "Icons.OpenWith";

    /// <inheritdoc/>
    public string RepoPath => _repoPath;

    /// <inheritdoc/>
    public bool IsLongRunning => false;

    /// <inheritdoc/>
    public bool RequiresBuildAnnotation => false;

    public LaunchEditorContributor(
        IEditorLauncherFactory launcherFactory,
        IConfigService configService,
        IPluginLogger? logger,
        string repoPath)
    {
        _launcherFactory = launcherFactory;
        _configService = configService;
        _logger = logger;
        _repoPath = repoPath;
    }

    /// <inheritdoc/>
    public bool IsVisible(CommitRef commit)
    {
        EnsureCached();
        return !string.IsNullOrEmpty(_cachedEditorPath);
    }

    /// <inheritdoc/>
    public bool IsEnabled(CommitRef commit)
    {
        EnsureCached();
        return !string.IsNullOrEmpty(_cachedEditorPath)
            && !string.IsNullOrEmpty(_cachedUprojectPath)
            && File.Exists(_cachedUprojectPath);
    }

    /// <inheritdoc/>
    public string? GroupKey => "UnrealSync";

    /// <inheritdoc/>
    public string? GroupHeader => "UnrealSync";

    /// <inheritdoc/>
    public string? GroupIconResourceKey => "avares://ugsgit/Resources/Images/unreal.png";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CommitRef commit, IProgress<string>? log, CancellationToken ct)
    {
        EnsureCached();

        if (string.IsNullOrEmpty(_cachedEditorPath))
            throw new InvalidOperationException("Editor binary not found. Sync or build the editor first.");

        if (string.IsNullOrEmpty(_cachedUprojectPath) || !File.Exists(_cachedUprojectPath))
            throw new FileNotFoundException("Project file (.uproject) not found.", _cachedUprojectPath);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _cachedEditorPath,
            Arguments = $"\"{_cachedUprojectPath}\"",
            UseShellExecute = true
        };

        var process = await Task.Run(() =>
        {
            try
            {
                return System.Diagnostics.Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                throw new FileNotFoundException(
                    $"Editor could not be launched: {_cachedEditorPath}", ex);
            }
        }, ct);

        _logger?.Log(
            process is { HasExited: false }
                ? $"[Launch Editor] Started for {_cachedProjectName} (PID: {process.Id})"
                : $"[Launch Editor] Editor process failed to start or exited immediately");
    }

    private void EnsureCached()
    {
        if (_configCached) return;

        var config = _configService.LoadConfig(_repoPath);

        // Derive project name from config (binaryName or projectFile)
        _cachedProjectName = config.BinaryName;
        if (string.IsNullOrEmpty(_cachedProjectName) && !string.IsNullOrEmpty(config.Engine?.ProjectFile))
            _cachedProjectName = Path.GetFileNameWithoutExtension(config.Engine.ProjectFile);

        // Derive .uproject path
        if (!string.IsNullOrEmpty(config.Engine?.ProjectFile))
        {
            _cachedUprojectPath = Path.Combine(_repoPath, config.Engine.ProjectFile);
        }
        else
        {
            var uprojects = Directory.GetFiles(_repoPath, "*.uproject");
            if (uprojects.Length > 0)
                _cachedUprojectPath = uprojects[0];
        }

        if (string.IsNullOrEmpty(_cachedProjectName) && !string.IsNullOrEmpty(_cachedUprojectPath))
            _cachedProjectName = Path.GetFileNameWithoutExtension(_cachedUprojectPath);

        // Resolve engine path: config has priority; fallback is repo root
        // (engine binaries synced into repo by "Sync Editor" or built by IDE).
        var enginePath = config.Engine?.Path;
        if (string.IsNullOrEmpty(enginePath))
            enginePath = _repoPath;

        // Check if the editor binary actually exists at the resolved engine path
        try
        {
            var launcher = _launcherFactory.Create(enginePath);
            _cachedEditorPath = launcher.FindEditorPath();
        }
        catch (FileNotFoundException)
        {
            _cachedEditorPath = null;
        }
        catch (Exception ex)
        {
            _logger?.Log($"[Launch Editor] Error resolving engine path: {ex.Message}");
            _cachedEditorPath = null;
        }

        _configCached = true;
    }
}
