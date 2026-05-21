using System;
using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Build configuration enum — mirrors UGS's build config dropdown.
/// </summary>
public enum UgsBuildConfig
{
    /// <summary>Debug configuration — no optimization, full debug symbols.</summary>
    Debug,
    /// <summary>DebugGame configuration — optimized game code with debug symbols for editor.</summary>
    DebugGame,
    /// <summary>Development configuration — optimized with some debug info, default for editor builds.</summary>
    Development,
    /// <summary>Test configuration — pre-shipping validation build.</summary>
    Test,
    /// <summary>Shipping configuration — fully optimized, no debug symbols, for distribution.</summary>
    Shipping
}

/// <summary>
/// Result status of a build operation.
/// Moved from BuildService to Models per L-4 fix.
/// </summary>
public enum BuildStatus
{
    /// <summary>Build completed successfully.</summary>
    Success,
    /// <summary>Build failed with errors.</summary>
    Failed,
    /// <summary>Build was cancelled by the user or system.</summary>
    Cancelled,
    /// <summary>Build exceeded the configured timeout limit.</summary>
    Timeout
}

/// <summary>
/// Structured result from a build operation.
/// Moved from BuildService to Models per L-4 fix.
/// </summary>
/// <param name="Status">Overall result status of the build operation.</param>
/// <param name="StepId">Identifier of the build step that produced this result.</param>
/// <param name="Message">Human-readable status or error message describing the outcome.</param>
/// <param name="Duration">Elapsed time for the build operation. Null if not measured or unavailable.</param>
public record BuildResult(
    BuildStatus Status,
    string StepId,
    string Message,
    TimeSpan? Duration = null
);
