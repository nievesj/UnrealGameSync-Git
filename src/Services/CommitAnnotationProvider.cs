using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Services
{
    /// <summary>
    /// Thread-safe provider that fans out annotation requests to all registered
    /// <see cref="ICommitAnnotator"/> instances and merges their results.
    /// </summary>
    public sealed class CommitAnnotationProvider : ICommitAnnotationProvider
    {
        private readonly List<ICommitAnnotator> _annotators = new List<ICommitAnnotator>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <inheritdoc/>
        public void Register(ICommitAnnotator annotator)
        {
            if (annotator == null)
                return;

            _lock.EnterWriteLock();
            try
            {
                if (!_annotators.Contains(annotator))
                    _annotators.Add(annotator);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public void Unregister(ICommitAnnotator annotator)
        {
            if (annotator == null)
                return;

            _lock.EnterWriteLock();
            try
            {
                _annotators.Remove(annotator);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>> GetAnnotationsAsync(
            IReadOnlyList<string> commitShas, CancellationToken ct)
        {
            if (commitShas == null || commitShas.Count == 0)
                return new Dictionary<string, IReadOnlyList<CommitAnnotation>>();

            List<ICommitAnnotator> snapshot;
            _lock.EnterReadLock();
            try
            {
                snapshot = _annotators.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (snapshot.Count == 0)
                return new Dictionary<string, IReadOnlyList<CommitAnnotation>>();

            var commitRefs = commitShas
                .Select(sha => new CommitRef(sha))
                .ToList();

            var tasks = snapshot.Select(annotator => InvokeAnnotatorAsync(annotator, commitRefs, ct));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var merged = new Dictionary<string, IReadOnlyList<CommitAnnotation>>(
                commitShas.Count, StringComparer.Ordinal);

            foreach (var result in results)
            {
                if (result == null)
                    continue;

                foreach (var kvp in result)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                        continue;

                    if (merged.TryGetValue(kvp.Key, out var existing))
                    {
                        merged[kvp.Key] = existing.Concat(kvp.Value).ToList();
                    }
                    else
                    {
                        merged[kvp.Key] = kvp.Value;
                    }
                }
            }

            return merged;
        }

        private static async Task<IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>> InvokeAnnotatorAsync(
            ICommitAnnotator annotator, IReadOnlyList<CommitRef> commits, CancellationToken ct)
        {
            try
            {
                return await annotator.AnnotateAsync(commits, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Respect cancellation — don't log as an error.
                return new Dictionary<string, IReadOnlyList<CommitAnnotation>>();
            }
            catch (Exception ex)
            {
                Native.OS.LogException(ex);
                return new Dictionary<string, IReadOnlyList<CommitAnnotation>>();
            }
        }
    }
}
