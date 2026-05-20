using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Model for UE's Engine/Build/Build.version JSON file.
/// Used by EngineInfoService to determine engine version.
/// </summary>
public class BuildVersion
{
    [JsonPropertyName("MajorVersion")] public int MajorVersion { get; init; }
    [JsonPropertyName("MinorVersion")] public int MinorVersion { get; init; }
    [JsonPropertyName("PatchVersion")] public int PatchVersion { get; init; }
    [JsonPropertyName("Changelist")] public int Changelist { get; init; }
    [JsonPropertyName("IsLicenseeVersion")] public int IsLicenseeVersion { get; init; }
    [JsonPropertyName("BranchName")] public string BranchName { get; init; } = string.Empty;
}
