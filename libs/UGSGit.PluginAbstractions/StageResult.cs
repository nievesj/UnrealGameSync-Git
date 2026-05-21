using System;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Structured result from BuildGraph staging operation.
/// Uses a dedicated StagingDirectory field instead of encoding the path
/// in BuildResult.Message (fixes council finding C-1: fragile string parsing).
/// </summary>
/// <param name="Status">Overall status of the staging operation.</param>
/// <param name="StepId">Identifier of the BuildGraph step that produced this result.</param>
/// <param name="StagingDirectory">Absolute path to the staged output directory.</param>
/// <param name="Message">Human-readable status or error message from the staging operation.</param>
/// <param name="Duration">Elapsed time for the staging step, or null if not measured.</param>
public record StageResult(
    BuildStatus Status,
    string StepId,
    string StagingDirectory,
    string Message,
    TimeSpan? Duration = null
);
