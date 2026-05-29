using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Commit context menu contributor that adds a "Sync Editor" item
/// to deploy prebuilt editor binaries from a network share.
/// Holds its own service references and caches config for fast IsVisible/IsEnabled checks.
/// </summary>
public class SyncEditorContributor : ICommitMenuContributor
{
    private readonly IDeployService _deployService;
    private readonly IConfigService _configService;
    private readonly IPluginLogger? _logger;
    private readonly string _repoPath;

    // Cached config values (lazy-initialized on first access)
    private string? _cachedNetworkBase;
    private string? _cachedProjectName;
    private string? _cachedEditorChannel;
    private bool _configCached;

    /// <inheritdoc/>
    public string Header => "Sync Editor";

    /// <inheritdoc/>
    public string? IconResourceKey => "Icons.Fetch";

    /// <inheritdoc/>
    public string RepoPath => _repoPath;

    /// <inheritdoc/>
    public bool RequiresBuildAnnotation => true;

    /// <summary>
    /// Creates a new SyncEditorContributor for the specified repository.
    /// </summary>
    /// <param name="deployService">Service for deploying editor binaries from network shares.</param>
    /// <param name="configService">Service for loading UnrealSync config.</param>
    /// <param name="logger">Optional logger for warnings and errors.</param>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    /// <param name="projectName">Project name derived from the .uproject file or BinaryName config.</param>
    public SyncEditorContributor(
        IDeployService deployService,
        IConfigService configService,
        IPluginLogger? logger,
        string repoPath,
        string projectName)
    {
        _deployService = deployService;
        _configService = configService;
        _logger = logger;
        _repoPath = repoPath;
        _cachedProjectName = projectName;
    }

    /// <inheritdoc/>
    public bool IsVisible(CommitRef commit)
    {
        EnsureCached();
        return !string.IsNullOrEmpty(_cachedNetworkBase);
    }

    /// <inheritdoc/>
    public bool IsEnabled(CommitRef commit)
    {
        // The host checks commit.Annotations for "build-available" to determine enabled state.
        // This contributor just confirms it's visible and applicable.
        return IsVisible(commit);
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(CommitRef commit, IProgress<string>? log, CancellationToken ct)
    {
        EnsureCached();

        var shortSha = commit.ShortSha;

        // Tee progress: forward to host-provided log (progress popup) AND plugin logger
        var progress = new Progress<string>(msg =>
        {
            log?.Report(msg);
            _logger?.Log($"[Sync Editor] {msg}");
        });

        var result = await _deployService.DeployAsync(
            _repoPath,
            _cachedNetworkBase!,
            _cachedEditorChannel ?? "Editor",
            _cachedProjectName ?? "Project",
            shortSha,
            progress,
            ct);

        // Report final status through both channels
        var statusMsg = $"{result.Status}: {result.Message}";
        log?.Report(statusMsg);
        _logger?.Log($"[Sync Editor] {statusMsg}");

        // DeployService may return non-success statuses — throw to signal error to the popup
        if (result.Status != DeployStatus.Success)
        {
            throw new InvalidOperationException(
                result.Message ?? $"Deploy returned status: {result.Status}");
        }
    }

    /// <inheritdoc/>
    public string? GroupKey => "UnrealSync";

    /// <inheritdoc/>
    public string? GroupHeader => "UnrealSync";

    /// <inheritdoc/>
    public string? GroupIconResourceKey => "avares://ugsgit/Resources/Images/unreal.png";

    /// <summary>
    /// Lazily caches config values on first access to avoid loading config on every right-click.
    /// </summary>
    private void EnsureCached()
    {
        if (_configCached) return;

        var config = _configService.LoadConfig(_repoPath);
        _cachedNetworkBase = config.NetworkBase;
        _cachedEditorChannel = config.EditorChannel;

        // BinaryName overrides project name for zip file matching
        if (!string.IsNullOrEmpty(config.BinaryName))
            _cachedProjectName = config.BinaryName;
        else if (!string.IsNullOrEmpty(config.Engine?.ProjectFile))
            _cachedProjectName = Path.GetFileNameWithoutExtension(config.Engine.ProjectFile);

        _configCached = true;
    }
}
