using System;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Publish abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.PublishService.
/// </summary>
public interface IPublishService
{
    /// <summary>
    /// Publishes a ZIP archive to the network share asynchronously.
    /// </summary>
    /// <param name="localZipPath">Absolute path to the local ZIP file to publish.</param>
    /// <param name="networkBaseUrl">Base URL or UNC path of the network share destination.</param>
    /// <param name="channel">Name of the publish channel (e.g. <c>dev</c>, <c>release</c>).</param>
    /// <param name="atomic">If <c>true</c>, publish atomically by writing to a temporary path and renaming on completion.</param>
    /// <param name="progress">An <see cref="IProgress{T}"/> to receive progress updates of type <see cref="PublishProgress"/>.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the publish operation.</param>
    /// <returns>A <see cref="PublishResult"/> indicating the outcome of the publish operation.</returns>
    Task<PublishResult> PublishZipAsync(
        string localZipPath,
        string networkBaseUrl,
        string channel,
        bool atomic,
        IProgress<PublishProgress> progress,
        CancellationToken ct = default);
}

/// <summary>
/// Reports progress of a publish operation.
/// </summary>
/// <param name="BytesCopied">Number of bytes copied so far.</param>
/// <param name="TotalBytes">Total number of bytes to copy.</param>
public record PublishProgress(long BytesCopied, long TotalBytes);

/// <summary>
/// Describes the outcome of a publish operation.
/// </summary>
/// <param name="Status">The overall publish status.</param>
/// <param name="Message">A human-readable message providing additional detail about the result.</param>
public record PublishResult(PublishStatus Status, string Message);

/// <summary>
/// Represents the possible outcomes of a publish operation.
/// </summary>
public enum PublishStatus
{
    /// <summary>The publish completed successfully.</summary>
    Success,

    /// <summary>The publish failed due to an error.</summary>
    Failed,

    /// <summary>The publish was cancelled by the user or by a timeout.</summary>
    Cancelled,

    /// <summary>No files needed publishing; the archive was up-to-date.</summary>
    NothingToPublish
}
