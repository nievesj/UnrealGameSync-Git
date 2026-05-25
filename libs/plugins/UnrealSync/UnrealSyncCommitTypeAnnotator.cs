using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Classifies commits as code/content by scanning their changed file extensions.
/// Code = any file whose modification may require recompilation or affect build configuration.
/// Content = UE assets (.uasset, .umap) and all other non-code files.
/// A commit can carry both Code and Content badges simultaneously.
/// <para>
/// Results are cached by (ShortSha, ConfigString) — since file classification is
/// deterministic for a given commit SHA and config, repeated graph refreshes
/// become dictionary lookups. Only new or uncached commits spawn git.exe processes.
/// The cache is invalidated when the config changes (different ConfigString).
/// </para>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> with a soft capacity limit
/// (default 4096 entries, matching original UGS). The capacity parameter is an initial
/// bucket count hint, not a hard maximum — the dictionary can grow beyond it.
/// When <see cref="ConcurrentDictionary{TKey,TValue}.Count"/> exceeds the capacity,
/// the entire cache is cleared rather than evicting individual entries — this
/// is safe because the next refresh will repopulate only the visible commits.
/// </para>
/// </summary>
public class UnrealSyncCommitTypeAnnotator : ICommitAnnotator
{
    private const int DefaultCacheCapacity = 4096;

    private readonly IGitFileQueryService _gitQuery;
    private readonly IConfigService _configService;
    private readonly IPluginLogger? _logger;
    private readonly string _repoPath;
    private readonly int _cacheCapacity;

    /// <summary>
    /// Thread-safe cache keyed by ShortSha. Since classification is deterministic
    /// for a given commit + config, cached entries never need TTL-based invalidation.
    /// Invalidated only when the config string changes or capacity is exceeded.
    /// </summary>
    private readonly ConcurrentDictionary<string, IReadOnlyList<CommitAnnotation>> _cache;
    private string _cachedConfigString = string.Empty;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Cache of compiled glob regex patterns to avoid recompiling on every call (M2).
    /// Cleared alongside the main cache on config string change.
    /// </summary>
    private readonly ConcurrentDictionary<string, Regex> _globRegexCache = new();

    public UnrealSyncCommitTypeAnnotator(
        IGitFileQueryService gitQuery,
        IConfigService configService,
        IPluginLogger? logger,
        string repoPath,
        int cacheCapacity = DefaultCacheCapacity)
    {
        _gitQuery = gitQuery;
        _configService = configService;
        _logger = logger;
        _repoPath = repoPath;
        _cacheCapacity = cacheCapacity > 0 ? cacheCapacity : DefaultCacheCapacity;
        _cache = new ConcurrentDictionary<string, IReadOnlyList<CommitAnnotation>>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: _cacheCapacity);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>> AnnotateAsync(
        IReadOnlyList<CommitRef> commits, CancellationToken ct)
    {
        var result = new Dictionary<string, IReadOnlyList<CommitAnnotation>>();

        var shas = commits.Select(c => c.ShortSha).Distinct().ToList();
        if (shas.Count == 0)
            return result;

        var config = _configService.LoadConfig(_repoPath);
        var configString = BuildConfigString(config);

        // Check if config changed — if so, invalidate entire cache (H4: lock for atomicity)
        lock (_cacheLock)
        {
            if (configString != _cachedConfigString)
            {
                _cache.Clear();
                _globRegexCache.Clear();
                _cachedConfigString = configString;
            }
        }

        // If cache exceeded soft capacity, clear and rebuild (rare — only on very large repos)
        if (_cache.Count > _cacheCapacity)
        {
            _cache.Clear();
        }

        // Separate cached vs uncached SHAs
        var uncachedShas = new List<string>();
        foreach (var sha in shas)
        {
            if (_cache.TryGetValue(sha, out var cached))
                result[sha] = cached;
            else
                uncachedShas.Add(sha);
        }

        // Only query git for uncached commits
        if (uncachedShas.Count > 0)
        {
            IReadOnlyDictionary<string, IReadOnlyList<string>> fileLists;
            try
            {
                fileLists = await _gitQuery.GetChangedFilesAsync(uncachedShas, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return result;
            }
            catch (Exception ex)
            {
                _logger?.Log($"Commit type annotation failed: {ex.Message}");
                return result;
            }

            var codeExtensions = BuildCodeExtensions(config.ChangeType);

            foreach (var sha in uncachedShas)
            {
                if (!fileLists.TryGetValue(sha, out var files) || files.Count == 0)
                    continue;

                // Single-pass classification with early exit (M3)
                bool containsCode = false, containsContent = false;
                foreach (var f in files)
                {
                    if (IsCodeFile(f, codeExtensions, config.ChangeType))
                        containsCode = true;
                    else
                        containsContent = true;
                    if (containsCode && containsContent) break;
                }

                var annotations = new List<CommitAnnotation>();
                if (containsCode)
                    annotations.Add(new CommitAnnotation("Code", "Contains code changes", CommitAnnotationTypes.CommitCode, config.CommitCodeBadgeColor));
                if (containsContent)
                    annotations.Add(new CommitAnnotation("Content", "Contains content changes", CommitAnnotationTypes.CommitContent, config.CommitContentBadgeColor));

                if (annotations.Count > 0)
                {
                    var readOnlyAnnotations = annotations.AsReadOnly();
                    result[sha] = readOnlyAnnotations;

                    // Store in cache for future refreshes
                    _cache[sha] = readOnlyAnnotations;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Builds a string representation of the config fields that affect classification.
    /// When this string changes, the cache is invalidated.
    /// Uses direct string comparison instead of SHA256 (L2) — the config is small
    /// and string equality is cheaper than cryptographic hashing.
    /// </summary>
    private static string BuildConfigString(UgsConfig config)
    {
        var sb = new StringBuilder();

        // Badge colors affect annotation output
        sb.Append(config.CommitCodeBadgeColor ?? "");
        sb.Append('\0');
        sb.Append(config.CommitContentBadgeColor ?? "");
        sb.Append('\0');

        // ChangeType config affects classification
        if (config.ChangeType != null)
        {
            var ct = config.ChangeType;
            sb.Append(string.Join(",", ct.ExtraCodeExtensions));
            sb.Append('\0');
            sb.Append(string.Join(",", ct.ExcludeCodeExtensions));
            sb.Append('\0');
            sb.Append(string.Join(",", ct.ForceContentPaths));
            sb.Append('\0');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the effective set of code extensions by combining built-in defaults
    /// with extra/exclude overrides from config.
    /// </summary>
    private HashSet<string> BuildCodeExtensions(UgsChangeTypeConfig? config)
    {
        var extensions = new HashSet<string>(s_builtinCodeExtensions, StringComparer.OrdinalIgnoreCase);
        if (config != null)
        {
            foreach (var ext in config.ExtraCodeExtensions)
                extensions.Add(ext.StartsWith(".") ? ext : "." + ext);
            foreach (var ext in config.ExcludeCodeExtensions)
                extensions.Remove(ext.StartsWith(".") ? ext : "." + ext);
        }

        return extensions;
    }

    /// <summary>
    /// Determines whether a file path should be classified as code.
    /// Force-content paths override everything (use glob matcher).
    /// </summary>
    private bool IsCodeFile(string path, HashSet<string> codeExtensions, UgsChangeTypeConfig? config)
    {
        // Normalize to forward slashes (git convention)
        var normalizedPath = path.Replace('\\', '/');

        // Force-content paths override everything (use glob matcher)
        if (config?.ForceContentPaths != null)
        {
            foreach (var pattern in config.ForceContentPaths)
            {
                if (GlobMatches(pattern, normalizedPath))
                    return false;
            }
        }

        var ext = Path.GetExtension(path);
        return !string.IsNullOrEmpty(ext) && codeExtensions.Contains(ext);
    }

    /// <summary>
    /// Glob matcher supporting * (any non-slash chars) and ** (any path).
    /// Patterns and paths are normalized to forward slashes.
    /// Uses compiled regex cache to avoid recompilation on every call (M2).
    /// </summary>
    private bool GlobMatches(string pattern, string path)
    {
        var regex = _globRegexCache.GetOrAdd(pattern, p =>
        {
            var normalized = p.Replace('\\', '/');
            var regexPattern = "^" + Regex.Escape(normalized)
                .Replace(@"\*\*", ".*")
                .Replace(@"\*", "[^/]*")
                + "$";
            return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        });
        return regex.IsMatch(path);
    }

    /// <summary>
    /// Built-in code extensions matching UGS categories.
    /// </summary>
    private static readonly HashSet<string> s_builtinCodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".h", ".hpp", ".hxx", ".c", ".cpp", ".cxx", ".cc", ".cs",
        ".usf", ".ush", ".uplugin", ".uproject", ".inl", ".hlsl", ".hlsli",
        ".glsl", ".metal", ".mm", ".m",
        ".java", ".py", ".rb", ".pl", ".lua", ".js", ".ts"
    };
}
