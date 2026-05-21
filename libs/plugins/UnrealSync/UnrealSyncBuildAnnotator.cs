using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Annotates commits that have editor or game builds available on the network share.
/// Caches the build list for 60 seconds to avoid excessive SMB scans.
/// </summary>
public class UnrealSyncBuildAnnotator : ICommitAnnotator
{
    private readonly IDeployService _deployService;
    private readonly IConfigService _configService;
    private readonly string _repoPath;
    private readonly string _projectName;

    // Cache with 60-second TTL
    private IReadOnlyList<DeployBuildInfo>? _cachedEditorBuilds;
    private IReadOnlyList<DeployBuildInfo>? _cachedGameBuilds;
    private DateTime _editorCacheExpiry = DateTime.MinValue;
    private DateTime _gameCacheExpiry = DateTime.MinValue;
    private readonly object _cacheLock = new();

    public UnrealSyncBuildAnnotator(
        IDeployService deployService,
        IConfigService configService,
        string repoPath,
        string projectName)
    {
        _deployService = deployService;
        _configService = configService;
        _repoPath = repoPath;
        _projectName = projectName;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>> AnnotateAsync(
        IReadOnlyList<CommitRef> commits, CancellationToken ct)
    {
        var result = new Dictionary<string, IReadOnlyList<CommitAnnotation>>();

        var config = _configService.LoadConfig(_repoPath);
        if (string.IsNullOrEmpty(config.NetworkBase))
            return result;

        // Derive project name from .uproject file if configured, else fall back to directory name
        var projectName = _projectName;
        if (!string.IsNullOrEmpty(config.BinaryName))
        {
            projectName = config.BinaryName;
        }
        else if (!string.IsNullOrEmpty(config.Engine?.ProjectFile))
        {
            projectName = Path.GetFileNameWithoutExtension(config.Engine.ProjectFile);
        }

        var editorBuilds = await GetOrRefreshBuildsAsync(config.NetworkBase, config.EditorChannel, projectName, isGame: false, ct);
        var gameBuilds = await GetOrRefreshBuildsAsync(config.NetworkBase, config.GameChannel, projectName, isGame: true, ct);

        var editorShas = editorBuilds?.Select(b => b.ShortSha).ToHashSet() ?? new HashSet<string>();
        var gameShas = gameBuilds?.Select(b => b.ShortSha).ToHashSet() ?? new HashSet<string>();

        foreach (var sha in commits.Select(c => c.ShortSha).Distinct())
        {
            var annotations = new List<CommitAnnotation>();

            if (editorShas.Contains(sha))
            {
                annotations.Add(new CommitAnnotation(
                    Label: "Editor",
                    Tooltip: "Editor build available on network share",
                    AnnotationType: "build-available",
                    Color: config.EditorBadgeColor));
            }

            if (gameShas.Contains(sha))
            {
                annotations.Add(new CommitAnnotation(
                    Label: "Game",
                    Tooltip: "Game build available on network share",
                    AnnotationType: "game-available",
                    Color: config.GameBadgeColor));
            }

            if (annotations.Count > 0)
                result[sha] = annotations;
        }

        return result;
    }

    /// <summary>Invalidates the build caches. Call after sync/deploy operations.</summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedEditorBuilds = null;
            _cachedGameBuilds = null;
            _editorCacheExpiry = DateTime.MinValue;
            _gameCacheExpiry = DateTime.MinValue;
        }
    }

    private async Task<IReadOnlyList<DeployBuildInfo>?> GetOrRefreshBuildsAsync(
        string networkBase, string channel, string projectName, bool isGame, CancellationToken ct)
    {
        lock (_cacheLock)
        {
            if (isGame)
            {
                if (_cachedGameBuilds != null && DateTime.UtcNow < _gameCacheExpiry)
                    return _cachedGameBuilds;
            }
            else
            {
                if (_cachedEditorBuilds != null && DateTime.UtcNow < _editorCacheExpiry)
                    return _cachedEditorBuilds;
            }
        }

        try
        {
            var builds = await _deployService.DiscoverAsync(networkBase, channel, projectName, ct);

            lock (_cacheLock)
            {
                if (isGame)
                {
                    _cachedGameBuilds = builds;
                    _gameCacheExpiry = DateTime.UtcNow.AddSeconds(60);
                }
                else
                {
                    _cachedEditorBuilds = builds;
                    _editorCacheExpiry = DateTime.UtcNow.AddSeconds(60);
                }
            }

            return builds;
        }
        catch
        {
            // Network unreachable — return empty, don't crash the graph
            return null;
        }
    }
}