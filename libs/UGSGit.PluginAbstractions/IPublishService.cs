using System;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.Models;

/// <summary>
/// Publish abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.PublishService.
/// </summary>
public interface IPublishService
{
    Task<PublishResult> PublishZipAsync(
        string localZipPath,
        string networkBaseUrl,
        string channel,
        bool atomic,
        IProgress<PublishProgress> progress,
        CancellationToken ct = default);
}

public record PublishProgress(long BytesCopied, long TotalBytes);
public record PublishResult(PublishStatus Status, string Message);
public enum PublishStatus { Success, Failed, Cancelled, NothingToPublish }
