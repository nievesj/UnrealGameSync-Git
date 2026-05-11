using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using SourceGit.Models;

namespace SourceGit.Services;

/// <summary>
/// Executes UBT build targets or custom scripts. Has CancellationToken support
/// and configurable process timeout (default: 2 hours for UE builds).
/// Fixes RC-7.
/// </summary>
public class BuildService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromHours(2);

    private readonly string _repoPath;

    public BuildService(string repoPath)
    {
        _repoPath = repoPath;
    }

    /// <summary>
    /// Execute a single build step. Streams output to progress.
    /// </summary>
    public async Task<BuildResult> ExecuteStepAsync(
        UgsBuildStep step,
        IProgress<string> log,
        CancellationToken ct,
        TimeSpan? timeout = null)
    {
        log.Report($"\n> Build: {step.DisplayName} ({step.Platform} {step.Configuration})...");

        var sw = Stopwatch.StartNew();

        Process process = null!;
        CancellationTokenSource linkedCts = null!;

        try
        {
            ProcessStartInfo psi;
            if (!string.IsNullOrEmpty(step.ScriptPath))
            {
                var fullPath = Path.IsPathRooted(step.ScriptPath)
                    ? step.ScriptPath
                    : Path.GetFullPath(Path.Combine(_repoPath, step.ScriptPath));
                psi = new ProcessStartInfo(fullPath)
                {
                    WorkingDirectory = _repoPath
                };
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c echo [UnrealSync] Build target '{step.Target}' is not yet wired to UBT. Build step was skipped. && exit 1",
                    WorkingDirectory = _repoPath
                };
            }

            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            process = new Process { StartInfo = psi };

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(timeout ?? DefaultTimeout);

            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            var stderr = await process.StandardError.ReadToEndAsync(linkedCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

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
            KillProcess(process);
            sw.Stop();
            return new BuildResult(BuildStatus.Cancelled, step.Id,
                "Build cancelled", sw.Elapsed);
        }
        catch (Exception ex)
        {
            KillProcess(process);
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

    private static void KillProcess(Process process)
    {
        if (process is { HasExited: false })
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Execute steps in order. Stops on first failure.
    /// </summary>
    public async Task<BuildResult> ExecuteAllAsync(
        System.Collections.Generic.List<UgsBuildStep> steps,
        IProgress<string> log,
        CancellationToken ct)
    {
        foreach (var step in steps)
        {
            var result = await ExecuteStepAsync(step, log, ct);
            if (result.Status != BuildStatus.Success)
                return result;
        }
        return new BuildResult(BuildStatus.Success, "all", "All build steps completed");
    }
}

public enum BuildStatus
{
    Success,
    Failed,
    Cancelled,
    Timeout
}

public record BuildResult(
    BuildStatus Status,
    string StepId,
    string Message,
    TimeSpan? Duration = null
);
