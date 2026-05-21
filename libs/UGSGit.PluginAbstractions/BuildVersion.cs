using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Model for UE's Engine/Build/Build.version JSON file.
/// Used by EngineInfoService to determine engine version.
/// </summary>
public class BuildVersion
{
    /// <summary>Major engine version number (e.g. 5 for UE5).</summary>
    [JsonPropertyName("MajorVersion")] public int MajorVersion { get; init; }

    /// <summary>Minor engine version number (e.g. 3 for UE 5.3).</summary>
    [JsonPropertyName("MinorVersion")] public int MinorVersion { get; init; }

    /// <summary>Patch version number for the engine release.</summary>
    [JsonPropertyName("PatchVersion")] public int PatchVersion { get; init; }

    /// <summary>Changelist number from which this build was compiled.</summary>
    [JsonPropertyName("Changelist")] public int Changelist { get; init; }

    /// <summary>Non-zero if this is a licensee (private) changelist rather than an Epic changelist.</summary>
    [JsonPropertyName("IsLicenseeVersion")] public int IsLicenseeVersion { get; init; }

    /// <summary>Name of the branch from which this build was produced.</summary>
    [JsonPropertyName("BranchName")] public string BranchName { get; init; } = string.Empty;
}
