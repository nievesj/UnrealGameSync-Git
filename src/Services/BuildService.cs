#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;

namespace SourceGit.Services;

/// <summary>
/// Executes UBT build targets or custom scripts. Has CancellationToken support
/// and configurable process timeout (default: 2 hours for UE builds).
/// Fixes RC-7.
/// </summary>
public class BuildService : IBuildService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromHours(2);

    private readonly string _repoPath;
    private readonly string _enginePath;
    private readonly string _uprojectPath;

    public BuildService(string repoPath, string enginePath, string uprojectPath)
    {
        _repoPath = repoPath;
        _enginePath = enginePath;
        _uprojectPath = uprojectPath;
    }

    /// <summary>
    /// Execute a single build step. Streams output to progress.
    /// </summary>
    public async Task<BuildResult> ExecuteStepAsync(
        UgsBuildStep step,
        IProgress<string> log,
        CancellationToken ct,
        TimeSpan? timeout = null,
        string? archiveDir = null)
    {
        log.Report($"\n> Build: {step.DisplayName} ({step.Platform} {step.Configuration})...");

        var sw = Stopwatch.StartNew();

        Process process = null!;
        CancellationTokenSource linkedCts = null!;

        try
        {
            ProcessStartInfo psi;
            if (string.IsNullOrEmpty(step.ScriptPath))
            {
                return new BuildResult(BuildStatus.Failed, step.Id,
                    "Build error: Script path is empty. No build target configured.");
            }

            // --- Expand template variables ---
            var projectName = Path.GetFileNameWithoutExtension(
                string.IsNullOrEmpty(_uprojectPath) ? "Project" : _uprojectPath);
            var uprojectPath = string.IsNullOrEmpty(_uprojectPath) ? "" : Path.GetFullPath(_uprojectPath);

            var expandedScriptPath = step.ScriptPath.Replace("{ProjectName}", projectName, StringComparison.OrdinalIgnoreCase);
            expandedScriptPath = expandedScriptPath.Replace("{EnginePath}", _enginePath, StringComparison.OrdinalIgnoreCase);
            var expandedTarget = step.Target.Replace("{ProjectName}", projectName, StringComparison.OrdinalIgnoreCase);
            expandedTarget = expandedTarget.Replace("{EnginePath}", _enginePath, StringComparison.OrdinalIgnoreCase);

            var expandedArguments = (!string.IsNullOrEmpty(step.Arguments) ? step.Arguments : "")
                .Replace("{Target}", expandedTarget, StringComparison.OrdinalIgnoreCase)
                .Replace("{UbtTarget}", expandedTarget, StringComparison.OrdinalIgnoreCase)
                .Replace("{Platform}", step.Platform, StringComparison.OrdinalIgnoreCase)
                .Replace("{Configuration}", step.Configuration, StringComparison.OrdinalIgnoreCase)
                .Replace("{ProjectName}", projectName, StringComparison.OrdinalIgnoreCase)
                .Replace("{ProjectPath}", uprojectPath, StringComparison.OrdinalIgnoreCase)
                .Replace("{EnginePath}", _enginePath, StringComparison.OrdinalIgnoreCase)
                .Replace("{ArchiveDir}", archiveDir ?? Path.Combine(_repoPath, "Saved", "StagedBuilds"), StringComparison.OrdinalIgnoreCase);

            // --- ScriptPath validation (fixes C-1) ---

            // 1. Check extension is in allowlist
            var ext = Path.GetExtension(expandedScriptPath);
            var allowedExtensions = new[] { ".bat", ".cmd", ".sh", ".ps1" };
            if (!allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                return new BuildResult(BuildStatus.Failed, step.Id,
                    $"Build error: Script path '{expandedScriptPath}' has invalid extension '{ext}'. Allowed: .bat, .cmd, .sh, .ps1");
            }

            // 2. Resolve full path and verify it's within _repoPath directory tree
            //    (unless the path uses {EnginePath}, which may resolve outside the repo)
            var usesEnginePath = step.ScriptPath.StartsWith("{EnginePath}", StringComparison.OrdinalIgnoreCase);
            var fullPath = Path.IsPathRooted(expandedScriptPath)
                ? Path.GetFullPath(expandedScriptPath)
                : Path.GetFullPath(Path.Combine(_repoPath, expandedScriptPath));

            if (!usesEnginePath)
            {
                var repoDir = Path.GetFullPath(_repoPath);
                if (!repoDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    repoDir += Path.DirectorySeparatorChar;

                if (!fullPath.StartsWith(repoDir, StringComparison.OrdinalIgnoreCase))
                {
                    return new BuildResult(BuildStatus.Failed, step.Id,
                        $"Build error: Script path '{expandedScriptPath}' resolves outside the repository directory '{_repoPath}'.");
                }
            }

            // 3. Reject shell metacharacters (check raw unexpanded path, not expanded)
            if (step.ScriptPath.IndexOfAny(new[] { '&', '|', ';', '`', '\n' }) >= 0 ||
                step.ScriptPath.Contains("$(", StringComparison.Ordinal) ||
                step.ScriptPath.Contains("${", StringComparison.Ordinal))
            {
                return new BuildResult(BuildStatus.Failed, step.Id,
                    $"Build error: Script path '{expandedScriptPath}' contains shell metacharacters.");
            }

            psi = new ProcessStartInfo(fullPath)
            {
                WorkingDirectory = _repoPath,
                Arguments = expandedArguments
            };

            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            // Pass expanded target as an environment variable so the script can use it
            psi.EnvironmentVariables["UBT_TARGET"] = expandedTarget;
            psi.EnvironmentVariables["UBT_PLATFORM"] = step.Platform;
            psi.EnvironmentVariables["UBT_CONFIGURATION"] = step.Configuration;
            psi.EnvironmentVariables["UE_PROJECT_NAME"] = projectName;
            psi.EnvironmentVariables["UE_ENGINE_PATH"] = _enginePath;
            psi.EnvironmentVariables["UE_BUILD_MODE"] = step.BuildMode ?? BuildModes.Ubt;

            process = new Process { StartInfo = psi };

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(timeout ?? DefaultTimeout);

            process.Start();

            // Read stdout and stderr concurrently to avoid deadlock
            // (child process can fill stderr pipe buffer while we're reading stdout)
            var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);
            await process.WaitForExitAsync(linkedCts.Token);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            log.Report(stdout);
            if (!string.IsNullOrWhiteSpace(stderr)) log.Report(stderr);

            sw.Stop();

            if (process.ExitCode == 0)
                return new BuildResult(BuildStatus.Success, step.Id,
                    $"Build succeeded ({sw.Elapsed.TotalSeconds:F1}s)", sw.Elapsed);

            return new BuildResult(BuildStatus.Failed, step.Id,
                $"Build failed (exit {process.ExitCode})", sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            ProcessHelper.KillProcessTree(process);
            sw.Stop();
            return new BuildResult(BuildStatus.Cancelled, step.Id,
                "Build cancelled", sw.Elapsed);
        }
        catch (Exception ex)
        {
            ProcessHelper.KillProcessTree(process);
            sw.Stop();
            Native.OS.LogException(ex);
            return new BuildResult(BuildStatus.Failed, step.Id,
                $"Build error: {ex.Message}", sw.Elapsed);
        }
        finally
        {
            linkedCts?.Dispose();
            process?.Dispose();
        }
    }

    /// <summary>
    /// Execute steps in order. Stops on first failure.
    /// </summary>
    public async Task<BuildResult> ExecuteAllAsync(
        System.Collections.Generic.List<UgsBuildStep> steps,
        IProgress<string> log,
        CancellationToken ct,
        string? archiveDir = null)
    {
        foreach (var step in steps)
        {
            var result = await ExecuteStepAsync(step, log, ct, timeout: null, archiveDir: archiveDir);
            if (result.Status != BuildStatus.Success)
                return result;
        }
        return new BuildResult(BuildStatus.Success, "all", "All build steps completed");
    }
}

// BuildStatus and BuildResult moved to UGSGit.Models (fixes L-4).
