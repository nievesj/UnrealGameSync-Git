using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Read-only git query methods for file history. Does not modify the repository.
/// </summary>
public interface IGitFileQueryService
{
    /// <summary>
    /// Gets the list of changed files for each commit SHA.
    /// Uses per-commit <c>git diff-tree</c> calls which handle root commits
    /// and merge commits correctly.
    /// </summary>
    /// <param name="commitShas">List of full or short commit SHAs to query.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>Map of commit SHA → list of changed file paths (relative to repo root, forward slashes).</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetChangedFilesAsync(
        IReadOnlyList<string> commitShas, CancellationToken ct = default);
}
