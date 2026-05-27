#nullable enable

using System;
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
public class BuildGraphService : IBuildGraphService
{
    private readonly string _enginePath;
    private readonly string _repoPath;
    private readonly string _uprojectPath;
    private readonly string _shortSha;
    private readonly string _projectName;
    private readonly UgsConfig _config;

    public BuildGraphService(string enginePath, string repoPath, UgsConfig config,
        string uprojectPath = "", string shortSha = "", string projectName = "")
    {
        _enginePath = enginePath;
        _repoPath = repoPath;
        _config = config;
        _uprojectPath = uprojectPath;
        _shortSha = shortSha;
        _projectName = projectName;
    }

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
        string? setArgsTemplate = null)
    {
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
        Process process = null!;
        CancellationTokenSource linkedCts = null!;

        try
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(timeout ?? TimeSpan.FromHours(2));

            var psi = new ProcessStartInfo
            {
                FileName = runUat,
                Arguments = args,
                WorkingDirectory = _enginePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process = new Process { StartInfo = psi };
            process.Start();

            // Stream stdout line-by-line for live UI feedback
            var stdoutBuilder = new System.Text.StringBuilder();
            var stderrBuilder = new System.Text.StringBuilder();

            var stdoutTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();
                    var line = await process.StandardOutput.ReadLineAsync(linkedCts.Token).ConfigureAwait(false);
                    if (line != null)
                    {
                        stdoutBuilder.AppendLine(line);
                        log.Report(line + Environment.NewLine);
                    }
                }
            }, linkedCts.Token);

            var stderrTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();
                    var line = await process.StandardError.ReadLineAsync(linkedCts.Token).ConfigureAwait(false);
                    if (line != null)
                    {
                        stderrBuilder.AppendLine(line);
                    }
                }
            }, linkedCts.Token);

            await Task.WhenAll(stdoutTask, stderrTask).WaitAsync(linkedCts.Token);
            await process.WaitForExitAsync(linkedCts.Token);

            sw.Stop();
            var stdout = stdoutBuilder.ToString();
            var stderr = stderrBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(stderr)) log.Report(stderr);

            if (process.ExitCode != 0)
                return new StageResult(BuildStatus.Failed, "stage", string.Empty,
                    $"BuildGraph failed (exit {process.ExitCode})", sw.Elapsed);

            var stagingDir = GetStagingDirectory();
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
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            try
            {
                using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
                foreach (var file in Directory.GetFiles(stagingDir, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();

                    if (excludePdb && file.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var relativePath = Path.GetRelativePath(stagingDir, file);
                    archive.CreateEntryFromFile(file, relativePath, GetCompressionLevel());
                }
            }
            catch (OperationCanceledException) when (File.Exists(outputPath))
            {
                File.Delete(outputPath);
                throw;
            }
        }, ct);

        return outputPath;
    }

    private static CompressionLevel GetCompressionLevel() => CompressionLevel.Optimal;

    private string GetRunUatPath() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(_enginePath, "Engine", "Build", "BatchFiles", "RunUAT.bat")
            : Path.Combine(_enginePath, "Engine", "Build", "BatchFiles", "RunUAT.sh");

    /// <summary>
    /// Get staging directory from config (fixes H-2: uses cached _config, not LoadConfig per call).
    /// </summary>
    private string GetStagingDirectory()
    {
        var outputDir = _config.BuildDefaults?.OutputDirectory ?? "Saved/StagedBuilds";
        return Path.GetFullPath(Path.Combine(_repoPath, outputDir));
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
            .Replace("{ProjectPath}", _uprojectPath)
            .Replace("{ShortSha}", _shortSha)
            .Replace("{ProjectName}", _projectName);
    }
}
