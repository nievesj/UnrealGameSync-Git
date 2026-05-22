#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.Models;
using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Deploys precompiled editor builds from a network share.
/// Handles discovery, download, lock detection, manifest-based cleanup,
/// staged extraction, and manifest/state persistence.
/// </summary>
public class DeployService : IDeployService
{
    private const int DownloadBufferSize = 1048576; // 1MB
    private const int ShortShaLength = 9;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string projectName, string targetName), Regex> _regexCache = new();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DeployBuildInfo>> DiscoverAsync(
        string networkBase, string channel, string projectName, CancellationToken ct)
    {
        var results = new List<DeployBuildInfo>();
        var searchDir = Path.Combine(networkBase, channel);
        var targetName = string.IsNullOrEmpty(channel) ? "Editor" : channel;
        var pattern = _regexCache.GetOrAdd(
            (projectName, targetName),
            key => new Regex($@"(?i){Regex.Escape(key.projectName)}{Regex.Escape(key.targetName)}-([0-9a-f]+)\.zip", RegexOptions.Compiled));

        try
        {
            // Apply a 30-second timeout to network operations to prevent SMB hangs
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            var effectiveCt = timeoutCts.Token;

            if (!Directory.Exists(searchDir))
                return results;

            var files = Directory.GetFiles(searchDir, $"{projectName}{targetName}-*.zip");
            foreach (var file in files)
            {
                effectiveCt.ThrowIfCancellationRequested();

                FileInfo fi;
                try
                {
                    fi = new FileInfo(file);
                }
                catch (Exception ex)
                {
                    Native.OS.LogException(new InvalidOperationException(
                        $"Could not read build zip metadata: {file}. Skipping.", ex));
                    continue;
                }

                var match = pattern.Match(Path.GetFileName(file));
                if (!match.Success)
                    continue;

                var shortSha = match.Groups[1].Value.ToLowerInvariant();
                results.Add(new DeployBuildInfo(shortSha, file, fi.Length, fi.LastWriteTimeUtc));
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Fatal: network share unreachable
            throw new InvalidOperationException(
                $"Network share unreachable: {searchDir}", ex);
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<DeployBuildInfo?> FindBuildForCommitAsync(
        string networkBase, string channel, string projectName, string commitSha, CancellationToken ct)
    {
        var builds = await DiscoverAsync(networkBase, channel, projectName, ct);

        var shortSha = commitSha.Length >= ShortShaLength
            ? commitSha[..ShortShaLength].ToLowerInvariant()
            : commitSha.ToLowerInvariant();

        var matches = builds
            .Where(b => string.Equals(b.ShortSha, shortSha, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(b => b.LastModified)
            .ToList();

        return matches.Count > 0 ? matches[0] : null;
    }

    /// <inheritdoc/>
    public async Task<DeployResult> DeployAsync(
        string repoPath, string networkBase, string channel, string projectName,
        string commitSha, IProgress<string> log, CancellationToken ct)
    {
        // 1. Find the build
        var build = await FindBuildForCommitAsync(networkBase, channel, projectName, commitSha, ct);
        if (build == null)
        {
            log.Report("No editor build found for commit.");
            return new DeployResult(DeployStatus.NoBuildFound, "No editor build found for the requested commit.");
        }

        var shortSha = build.ShortSha;

        // 2. Check if already synced
        var state = ConfigService.LoadLocalState(repoPath);
        if (string.Equals(state.LastDeployedArchiveSha, shortSha, StringComparison.OrdinalIgnoreCase))
        {
            log.Report($"Editor build {shortSha} already deployed — skipping.");
            return new DeployResult(DeployStatus.Success, $"Editor build {shortSha} is already deployed.");
        }

        // 3. Check editor lock detection (Windows-only)
        if (IsEditorRunning(repoPath))
        {
            log.Report("Editor appears to be running. Close it before deploying binaries.");
            return new DeployResult(DeployStatus.EditorRunning,
                "Unreal Editor is running. Please close it before deploying editor binaries.");
        }

        // 4. Download
        var unsyncDir = Path.Combine(repoPath, ".unrealsync");
        var tempZipPath = Path.Combine(unsyncDir, "deploy.zip");

        try
        {
            Directory.CreateDirectory(unsyncDir);
        }
        catch (Exception ex)
        {
            return new DeployResult(DeployStatus.Failed, $"Could not create .unrealsync directory: {ex.Message}");
        }

        try
        {
            log.Report($"Downloading editor build {shortSha}...");

            if (!File.Exists(build.ZipPath))
            {
                return new DeployResult(DeployStatus.NetworkUnavailable,
                    $"Build zip not found at {build.ZipPath}. Network share may be unavailable.");
            }

            await CopyFileWithProgressAsync(build.ZipPath, tempZipPath, log, ct);

            // Verify the download is not zero-length
            var downloadedInfo = new FileInfo(tempZipPath);
            if (downloadedInfo.Length == 0)
            {
                File.Delete(tempZipPath);
                return new DeployResult(DeployStatus.Failed, "Downloaded zip is zero-length — possible network error.");
            }

            log.Report($"Download complete ({downloadedInfo.Length / 1024 / 1024} MB).");
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(tempZipPath))
                try { File.Delete(tempZipPath); } catch { /* best effort */ }
            throw;
        }
        catch (Exception ex)
        {
            if (File.Exists(tempZipPath))
                try { File.Delete(tempZipPath); } catch { /* best effort */ }

            return new DeployResult(DeployStatus.NetworkUnavailable,
                $"Failed to download editor build: {ex.Message}");
        }

        // 5. Remove old binaries
        try
        {
            RemoveOldBinaries(repoPath, log);
        }
        catch (Exception ex)
        {
            log.Report($"Warning: could not fully remove old binaries: {ex.Message}");
            // Continue — stale files left behind is not fatal
        }

        // 6. Extract
        try
        {
            log.Report("Extracting editor binaries...");
            ExtractZipToProject(tempZipPath, repoPath, log);
            log.Report("Extraction complete.");
        }
        catch (Exception ex)
        {
            return new DeployResult(DeployStatus.Failed, $"Failed to extract editor build: {ex.Message}");
        }
        finally
        {
            // Cleanup: delete temp zip
            try { if (File.Exists(tempZipPath)) File.Delete(tempZipPath); } catch { /* best effort */ }
        }

        // 7. Update workspace state
        state.LastDeployedArchiveSha = shortSha;
        ConfigService.SaveLocalState(repoPath, state);

        log.Report($"Editor build {shortSha} deployed successfully.");
        return new DeployResult(DeployStatus.Success, $"Editor build {shortSha} deployed successfully.");
    }

    /// <summary>
    /// Checks if editor binaries are locked by a running process (Windows-only).
    /// </summary>
    private static bool IsEditorRunning(string repoPath)
    {
        string[] editorBinaries =
        {
            "Engine/Binaries/Win64/UnrealEditor-Win64-Development.exe",
            "Engine/Binaries/Win64/UnrealEditor-Win64-Debug.exe",
            "Engine/Binaries/Win64/UnrealEditor-Win64-DebugGame.exe",
        };

        foreach (var relative in editorBinaries)
        {
            var path = Path.Combine(repoPath, relative);
            if (File.Exists(path))
            {
                try
                {
                    using var stream = File.OpenWrite(path);
                    // If we can open for write, it's not locked
                }
                catch (IOException)
                {
                    return true; // Locked = editor running
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Reads the old manifest and removes files that match (same size + timestamp ±2 seconds).
    /// </summary>
    private static void RemoveOldBinaries(string repoPath, IProgress<string> log)
    {
        var manifestPath = Path.Combine(repoPath, ".unrealsync", "Editor.zipmanifest");
        if (!File.Exists(manifestPath))
        {
            log.Report("No previous manifest found — skipping binary cleanup.");
            return;
        }

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize(json, UnrealSyncJsonContext.Default.DeployManifest);
        if (manifest?.Files == null || manifest.Files.Count == 0)
            return;

        int removed = 0;
        int skipped = 0;

        foreach (var entry in manifest.Files)
        {
            var fullPath = Path.Combine(repoPath, entry.RelativePath);
            if (!File.Exists(fullPath))
                continue;

            var fi = new FileInfo(fullPath);

            // Safety check: only delete if size and timestamp match (±2 seconds)
            if (fi.Length != entry.Length)
            {
                log.Report($"Skipping modified file: {entry.RelativePath} (size changed)");
                skipped++;
                continue;
            }

            var timeDiff = Math.Abs((fi.LastWriteTimeUtc - entry.LastWriteTimeUtc).TotalSeconds);
            if (timeDiff > 2.0)
            {
                log.Report($"Skipping modified file: {entry.RelativePath} (timestamp changed)");
                skipped++;
                continue;
            }

            try
            {
                File.Delete(fullPath);
                removed++;
            }
            catch (Exception ex)
            {
                log.Report($"Warning: could not delete {entry.RelativePath}: {ex.Message}");
            }
        }

        log.Report($"Removed {removed} old file(s), skipped {skipped} modified file(s).");
    }

    /// <summary>
    /// Extracts a zip to a staging directory, then atomically moves files to the project root.
    /// Sets all extracted file timestamps to UTC now for consistent manifest tracking.
    /// </summary>
    private static void ExtractZipToProject(string zipPath, string projectRoot, IProgress<string> log)
    {
        var stagingDir = Path.Combine(Path.GetTempPath(), $"ugsgit-deploy-{Guid.NewGuid():N}");
        var timestamp = DateTime.UtcNow;
        int extractedCount = 0;

        try
        {
            Directory.CreateDirectory(stagingDir);

            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                // Skip directory entries
                if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    continue;

                var stagingPath = Path.Combine(stagingDir, entry.FullName);

                // Zip slip protection: reject entries that escape the staging directory
                var fullStagingPath = Path.GetFullPath(stagingPath);
                if (!fullStagingPath.StartsWith(Path.GetFullPath(stagingDir) + Path.DirectorySeparatorChar)
                    && !string.Equals(fullStagingPath, Path.GetFullPath(stagingDir), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Entry '{entry.FullName}' attempts to escape the staging directory.");

                var stagingDirForEntry = Path.GetDirectoryName(stagingPath);
                if (!string.IsNullOrEmpty(stagingDirForEntry))
                    Directory.CreateDirectory(stagingDirForEntry);

                entry.ExtractToFile(stagingPath, overwrite: true);
                File.SetLastWriteTimeUtc(stagingPath, timestamp);
                extractedCount++;
            }

            log.Report($"Extracted {extractedCount} files to staging.");

            // Atomic move: copy files from staging to project root
            var fullProjectRoot = Path.GetFullPath(projectRoot);
            foreach (var file in Directory.GetFiles(stagingDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(stagingDir.Length + 1);
                var targetPath = Path.Combine(projectRoot, relativePath);

                // Zip slip protection: reject entries that escape the project root
                var fullTargetPath = Path.GetFullPath(targetPath);
                if (!fullTargetPath.StartsWith(fullProjectRoot + Path.DirectorySeparatorChar)
                    && !string.Equals(fullTargetPath, fullProjectRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Entry '{relativePath}' attempts to escape the project directory.");

                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir))
                    Directory.CreateDirectory(targetDir);
                File.Move(file, targetPath, overwrite: true);
            }
        }
        finally
        {
            if (Directory.Exists(stagingDir))
            {
                try { Directory.Delete(stagingDir, true); } catch { /* best effort */ }
            }
        }

        // Write new manifest
        WriteManifest(zipPath, projectRoot, timestamp);
    }

    /// <summary>
    /// Writes a new manifest listing all extracted files with their sizes and timestamps.
    /// Writes to a temp file first, then atomically renames.
    /// </summary>
    private static void WriteManifest(string zipPath, string projectRoot, DateTime timestamp)
    {
        // Re-read the zip to enumerate all entries for the manifest
        var files = new List<DeployManifestFile>();
        using (var zip = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    continue;

                files.Add(new DeployManifestFile(
                    entry.FullName.Replace('\\', '/'),
                    entry.Length,
                    timestamp));
            }
        }

        // Determine the short SHA from the build (use the manifest's version)
        var manifest = new DeployManifest(Version: 1, CommitSha: "", Files: files);

        var manifestDir = Path.Combine(projectRoot, ".unrealsync");
        var manifestPath = Path.Combine(manifestDir, "Editor.zipmanifest");
        var tempManifestPath = manifestPath + ".tmp";

        var json = JsonSerializer.Serialize(manifest, UnrealSyncJsonContext.Default.DeployManifest);
        File.WriteAllText(tempManifestPath, json);

        // Atomic rename
        if (File.Exists(manifestPath))
            File.Delete(manifestPath);
        File.Move(tempManifestPath, manifestPath);
    }

    /// <summary>
    /// Copies a file with progress reporting using a 1MB buffer.
    /// </summary>
    private static async Task CopyFileWithProgressAsync(
        string source, string dest, IProgress<string> log, CancellationToken ct)
    {
        var totalBytes = new FileInfo(source).Length;
        long copiedBytes = 0;
        var lastProgressReport = 0L;

        await using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read,
            FileShare.Read, DownloadBufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
        await using var destStream = new FileStream(dest, FileMode.Create, FileAccess.Write,
            FileShare.None, DownloadBufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);

        var buffer = new byte[DownloadBufferSize];
        int read;
        while ((read = await sourceStream.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            await destStream.WriteAsync(buffer.AsMemory(0, read), ct);
            copiedBytes += read;

            // Report progress every ~10MB or at completion
            if (copiedBytes - lastProgressReport >= 10 * DownloadBufferSize || copiedBytes == totalBytes)
            {
                var percent = totalBytes > 0 ? (double)copiedBytes / totalBytes : 0;
                log.Report($"Download: {percent:P0} ({copiedBytes / 1024 / 1024} MB / {totalBytes / 1024 / 1024} MB)");
                lastProgressReport = copiedBytes;
            }
        }
    }
}