using System;
using SourceGit.Services;

namespace SourceGit.Models;

/// <summary>
/// Structured result from BuildGraph staging operation.
/// Uses a dedicated StagingDirectory field instead of encoding the path
/// in BuildResult.Message (fixes council finding C-1: fragile string parsing).
/// </summary>
public record StageResult(
    BuildStatus Status,
    string StepId,
    string StagingDirectory,
    string Message,
    TimeSpan? Duration = null
);
