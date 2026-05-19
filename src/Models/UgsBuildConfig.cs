using System;
using System.Text.Json.Serialization;

namespace UGSGit.Models;

/// <summary>
/// Build configuration enum — mirrors UGS's build config dropdown.
/// </summary>
public enum UgsBuildConfig
{
    Debug,
    DebugGame,
    Development,
    Test,
    Shipping
}

/// <summary>
/// Result status of a build operation.
/// Moved from BuildService to Models per L-4 fix.
/// </summary>
public enum BuildStatus
{
    Success,
    Failed,
    Cancelled,
    Timeout
}

/// <summary>
/// Structured result from a build operation.
/// Moved from BuildService to Models per L-4 fix.
/// </summary>
public record BuildResult(
    BuildStatus Status,
    string StepId,
    string Message,
    TimeSpan? Duration = null
);
