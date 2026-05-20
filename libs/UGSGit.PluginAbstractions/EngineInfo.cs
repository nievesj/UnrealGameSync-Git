namespace UGSGit.PluginAbstractions;

/// <summary>
/// Detected engine version and build type information.
/// Populated by EngineInfoService from Engine/Build/Build.version.
/// </summary>
public class EngineInfo
{
    public int MajorVersion { get; init; }
    public int MinorVersion { get; init; }
    public int PatchVersion { get; init; }
    public int Changelist { get; init; }
    public string Version { get; init; } = "Unknown";
    public string BuildType { get; init; } = "Unknown";
    public bool IsSourceBuild { get; init; }
}
