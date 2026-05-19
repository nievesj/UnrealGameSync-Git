using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using SourceGit.Models;

namespace SourceGit.Services;

/// <summary>
/// Publishes packaged zip files to a network location (UNC or local path).
/// Supports atomic publish (temp → rename) and progress reporting.
/// Uses 1MB buffer for large binary files (fixes M-3: was 81920).
/// </summary>
public class PublishService
{
    /// <summary>
    /// Publish a zip to the network location.
    /// Atomic: writes to .publish-temp/ then renames on completion.
    /// Full publish path: {networkBaseUrl}/{channel}/{zipName}
    /// </summary>
    public async Task<PublishResult> PublishZipAsync(
        string localZipPath,
        string networkBaseUrl,
        string channel,
        bool atomic,
        IProgress<PublishProgress> progress,
        CancellationToken ct)
    {
        if (!File.Exists(localZipPath))
            return new PublishResult(PublishStatus.NothingToPublish, "Zip file not found");

        var zipName = Path.GetFileName(localZipPath);
        var destDir = Path.Combine(networkBaseUrl, channel);
        var finalPath = Path.Combine(destDir, zipName);

        try
        {
            Directory.CreateDirectory(destDir);

            if (atomic)
            {
                // Atomic write: copy to temp dir, then rename
                var tempDir = Path.Combine(destDir, $".publish-temp-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                Directory.CreateDirectory(tempDir);
                var tempPath = Path.Combine(tempDir, zipName);

                try
                {
                    await CopyFileWithProgressAsync(localZipPath, tempPath, progress, ct);

                    if (File.Exists(finalPath))
                        File.Delete(finalPath);
                    File.Move(tempPath, finalPath);
                }
                finally
                {
                    // Clean up temp dir (success or failure)
                    if (Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
                    }
                }
            }
            else
            {
                await CopyFileWithProgressAsync(localZipPath, finalPath, progress, ct);
            }

            // Write manifest sidecar
            var manifestPath = finalPath + ".manifest.json";
            var manifest = new PublishManifest
            {
                ZipName = zipName,
                Channel = channel,
                Timestamp = DateTime.UtcNow,
                FileSize = new FileInfo(finalPath).Length
            };
            File.WriteAllText(manifestPath,
                JsonSerializer.Serialize(manifest, UnrealSyncJsonContext.Default.PublishManifest));

            return new PublishResult(PublishStatus.Success, $"Published to {finalPath}");
        }
        catch (OperationCanceledException)
        {
            return new PublishResult(PublishStatus.Cancelled, "Publish cancelled");
        }
        catch (Exception ex)
        {
            Native.OS.LogException(ex);
            return new PublishResult(PublishStatus.Failed, $"Publish error: {ex.Message}");
        }
    }

    /// <summary>
    /// Copy a file with progress reporting. Uses 1MB buffer for large files (fixes M-3).
    /// </summary>
    private static async Task CopyFileWithProgressAsync(
        string source, string dest,
        IProgress<PublishProgress> progress, CancellationToken ct)
    {
        const int bufferSize = 1048576; // 1MB
        var totalBytes = new FileInfo(source).Length;
        long copiedBytes = 0;

        await using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
        await using var destStream = new FileStream(dest, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);

        var buffer = new byte[bufferSize];
        int read;
        while ((read = await sourceStream.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            await destStream.WriteAsync(buffer.AsMemory(0, read), ct);
            copiedBytes += read;
            progress?.Report(new PublishProgress(copiedBytes, totalBytes));
        }
    }
}

public record PublishProgress(long BytesCopied, long TotalBytes);
public record PublishResult(PublishStatus Status, string Message);

public enum PublishStatus
{
    Success,
    Failed,
    Cancelled,
    NothingToPublish
}
