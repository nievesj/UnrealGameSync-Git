namespace UGSGit.PluginAbstractions;

/// <summary>
/// Detected engine version and build type information.
/// Populated by EngineInfoService from Engine/Build/Build.version.
/// </summary>
public class EngineInfo
{
    /// <summary>Major engine version number (e.g. 5 for UE5).</summary>
    public int MajorVersion { get; init; }

    /// <summary>Minor engine version number (e.g. 3 for UE 5.3).</summary>
    public int MinorVersion { get; init; }

    /// <summary>Patch version number for the engine release.</summary>
    public int PatchVersion { get; init; }

    /// <summary>Changelist number from which this engine build was compiled.</summary>
    public int Changelist { get; init; }

    /// <summary>Human-readable version string, e.g. "5.3.2-12345678". Defaults to "Unknown".</summary>
    public string Version { get; init; } = "Unknown";

    /// <summary>Build type string, e.g. "Shipping", "Development", or "Unknown".</summary>
    public string BuildType { get; init; } = "Unknown";

    /// <summary>Whether this is a source-built engine rather than a precompiled binary release.</summary>
    public bool IsSourceBuild { get; init; }
}
