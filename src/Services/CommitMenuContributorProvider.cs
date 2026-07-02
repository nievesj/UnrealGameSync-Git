using System.Collections.Generic;
using System.Linq;
using System.Threading;

using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Thread-safe registry for commit context menu contributors.
/// Plugins register/unregister contributors when their tabs activate/deactivate.
/// The host queries contributors by repo path when building commit context menus.
/// </summary>
public sealed class CommitMenuContributorProvider : ICommitMenuContributorProvider
{
    private readonly List<ICommitMenuContributor> _contributors = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <inheritdoc/>
    public void Register(ICommitMenuContributor contributor)
    {
        if (contributor == null)
            return;

        _lock.EnterWriteLock();
        try
        {
            if (!_contributors.Contains(contributor))
                _contributors.Add(contributor);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void Unregister(ICommitMenuContributor contributor)
    {
        if (contributor == null)
            return;

        _lock.EnterWriteLock();
        try
        {
            _contributors.Remove(contributor);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ICommitMenuContributor> GetContributorsForRepo(string repoPath)
    {
        _lock.EnterReadLock();
        try
        {
            return _contributors
                .Where(c => string.Equals(c.RepoPath, repoPath, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}