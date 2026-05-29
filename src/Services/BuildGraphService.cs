#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Orchestrates UE BuildGraph for staging and packaging editor/game binaries.
/// Uses a structured StageResult (not string parsing) per council review finding C-1.
/// Accepts UgsConfig in constructor per finding H-2 (no per-call LoadConfig).
/// Validates RunUAT path existence per finding M-2.
/// </summary>
public class BuildGraphService(
    string enginePath,
    string repoPath,
    UgsConfig config,
    string uprojectPath = "",
    string shortSha = "",
    string projectName = "") : IBuildGraphService
{
    /// <summary>
    /// Run BuildGraph to compile + stage editor/game binaries.
    /// Returns StageResult with StagingDirectory on success (fixes C-1).
    /// </summary>
    public async Task<StageResult> StageAsync(
        string editorTarget,
        string platform,
        string configuration,
        bool includePdb,
        IProgress<string> log,
        CancellationToken ct,
        TimeSpan? timeout = null,
        string? buildGraphScript = null,
        string? buildGraphTarget = null,
        string? setArgsTemplate = null,
        int logBatchSize = 50)
    {
        if (logBatchSize < 1)
            logBatchSize = 1;

        var runUat = GetRunUatPath();

        // Validate RunUAT exists (fixes M-2)
        if (!File.Exists(runUat))
            return new StageResult(BuildStatus.Failed, "stage", string.Empty,
                $"RunUAT not found at {runUat}. Is this a source build?");

        // Resolve script, target, and -set: arguments with fallback to built-in defaults
        var script = !string.IsNullOrWhiteSpace(buildGraphScript)
            ? buildGraphScript
            : "Engine/Build/Graph/Examples/BuildEditorAndTools.xml";

        var target = !string.IsNullOrWhiteSpace(buildGraphTarget)
            ? buildGraphTarget
            : "Copy to Staging Directory";

        var setArgs = !string.IsNullOrWhiteSpace(setArgsTemplate)
            ? ExpandBuildGraphVariables(setArgsTemplate, editorTarget)
            : $"-set:EditorTarget={editorTarget} -set:ArchiveStream={editorTarget}";

        var args = $"BuildGraph " +
                   $"-Script=\"{script}\" " +
                   $"-Target=\"{target}\" " +
                   $"{setArgs} ";

        if (!includePdb)
            args += "-set:ExcludePdb=true ";

        var sw = Stopwatch.StartNew();
        Process? process = null;
        CancellationTokenSource? linkedCts = null;

        try
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(timeout ?? TimeSpan.FromHours(2));

            var psi = new ProcessStartInfo
            {
                FileName = runUat,
                Arguments = args,
                WorkingDirectory = enginePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process = new Process { StartInfo = psi };

            // Ensure staging directory exists before BuildGraph runs so it has a valid target.
            var stagingDir = GetStagingDirectory();
            if (!Directory.Exists(stagingDir))
                Directory.CreateDirectory(stagingDir);

            process.Start();

            // Stream stdout line-by-line with batching to avoid flooding the UI dispatcher.
            // BuildGraph can output thousands of lines per second; reporting each individually
            // causes the app to show "Not Responding" because the UI thread can't keep up.
            var stdoutBuilder = new System.Text.StringBuilder();
            var stderrBuilder = new System.Text.StringBuilder();

            // Extract non-disposable locals before lambdas to silence closure disposal warnings.
            var stdoutReader = process.StandardOutput;
            var stderrReader = process.StandardError;
            var token = linkedCts.Token;

            var stdoutTask = Task.Run(async () =>
            {
                var batch = new List<string>(logBatchSize);
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var line = await stdoutReader.ReadLineAsync(token).ConfigureAwait(false);
                    if (line == null) break;
                    stdoutBuilder.AppendLine(line);
                    batch.Add(line);
                    if (batch.Count >= logBatchSize)
                    {
                        log.Report(string.Join(Environment.NewLine, batch));
                        batch.Clear();
                    }
                }
                if (batch.Count > 0)
                    log.Report(string.Join(Environment.NewLine, batch));
            }, token);

            var stderrTask = Task.Run(async () =>
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var line = await stderrReader.ReadLineAsync(token).ConfigureAwait(false);
                    if (line == null) break;
                    stderrBuilder.AppendLine(line);
                }
            }, token);

            await Task.WhenAll(stdoutTask, stderrTask).WaitAsync(linkedCts.Token);
            await process.WaitForExitAsync(linkedCts.Token);

            // Kill orphaned UAT child/grandchild processes that may still hold file handles
            // (council finding C: only killed on error/cancel before, now on all paths).
            ProcessHelper.KillProcessTree(process);

            sw.Stop();
            var stderr = stderrBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(stderr)) log.Report(stderr);

            if (process.ExitCode != 0)
            {
                var errorSummary = !string.IsNullOrWhiteSpace(stderr)
                    ? ExtractLastErrorLine(stderr)
                    : $"exit {process.ExitCode}";
                return new StageResult(BuildStatus.Failed, "stage", string.Empty,
                    $"BuildGraph failed: {errorSummary}", sw.Elapsed);
            }

            // Verify staging directory was actually produced
            if (!Directory.Exists(stagingDir))
                return new StageResult(BuildStatus.Failed, "stage", string.Empty,
                    $"Staging directory not found: {stagingDir}");

            return new StageResult(BuildStatus.Success, "stage", stagingDir,
                $"Staged to {stagingDir}", sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            ProcessHelper.KillProcessTree(process);
            sw.Stop();
            return new StageResult(BuildStatus.Cancelled, "stage", string.Empty,
                "Stage cancelled", sw.Elapsed);
        }
        catch (Exception ex)
        {
            ProcessHelper.KillProcessTree(process);
            sw.Stop();
            Native.OS.LogException(ex);
            return new StageResult(BuildStatus.Failed, "stage", string.Empty,
                $"Stage error: {ex.Message}", sw.Elapsed);
        }
        finally
        {
            linkedCts?.Dispose();
            process?.Dispose();
        }
    }

    /// <summary>
    /// Create a zip from the staging directory. Returns the zip file path.
    /// If cancellation is requested mid-zip, the partial file is deleted (fixes M-4).
    /// </summary>
    public async Task<string> CreateZipAsync(
        string stagingDir,
        string outputPath,
        bool excludePdb,
        IProgress<string> log,
        CancellationToken ct)
    {
        log.Report($"Creating zip: {outputPath}");

        await Task.Run(() =>
        {
            // Retry loop for transient file locks — the output zip may still be held
            // by the UAT BuildGraph process or antivirus/indexing after it exits.
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            const int maxRetries = 20;
            const int delayMs = 500;

            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (File.Exists(outputPath))
                        File.Delete(outputPath);

                    using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
                    foreach (var file in Directory.GetFiles(stagingDir, "*", SearchOption.AllDirectories))
                    {
                        ct.ThrowIfCancellationRequested();

                        if (excludePdb && file.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var relativePath = Path.GetRelativePath(stagingDir, file);
                        archive.CreateEntryFromFile(file, relativePath, GetCompressionLevel());
                    }

                    return; // success
                }
                catch (OperationCanceledException) when (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                    throw;
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    log.Report($"Zip file locked (attempt {attempt + 1}/{maxRetries}), waiting {delayMs}ms...");
                    Thread.Sleep(delayMs);
                }
            }
        }, ct);

        return outputPath;
    }

    private static CompressionLevel GetCompressionLevel() => CompressionLevel.Optimal;

    private string GetRunUatPath() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(enginePath, "Engine", "Build", "BatchFiles", "RunUAT.bat")
            : Path.Combine(enginePath, "Engine", "Build", "BatchFiles", "RunUAT.sh");

    /// <summary>
    /// Get staging directory from config (fixes H-2: uses cached config, not LoadConfig per call).
    /// </summary>
    private string GetStagingDirectory()
    {
        var outputDir = config.BuildDefaults.OutputDirectory ?? "Saved/StagedBuilds";
        return Path.GetFullPath(Path.Combine(repoPath, outputDir));
    }

    /// <summary>
    /// Extract the last meaningful error line from stderr for concise failure reporting.
    /// Skips empty lines, AutomationTool exit summaries, and timing info.
    /// </summary>
    private static string ExtractLastErrorLine(string stderr)
    {
        var lines = stderr.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
                continue;
            if (line.StartsWith("AutomationTool", StringComparison.OrdinalIgnoreCase))
                continue;
            if (line.StartsWith("BUILD FAILED", StringComparison.OrdinalIgnoreCase))
                continue;
            if (line.StartsWith("Stage Failed:", StringComparison.OrdinalIgnoreCase))
                continue;
            if (line.StartsWith("(", StringComparison.OrdinalIgnoreCase) && line.Contains("for full exception trace"))
                continue;
            return line;
        }
        return "exit error";
    }

    /// <summary>
    /// Expand variable placeholders in a BuildGraph -set: argument template.
    /// Supported variables: {UbtTarget}, {ProjectPath}, {ShortSha}, {ProjectName}.
    /// Unknown variables are left as-is.
    /// </summary>
    private string ExpandBuildGraphVariables(string template, string ubtTarget)
    {
        return template
            .Replace("{UbtTarget}", ubtTarget)
            .Replace("{ProjectPath}", uprojectPath)
            .Replace("{ShortSha}", shortSha)
            .Replace("{ProjectName}", projectName);
    }
}
