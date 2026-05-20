using System.Text.Json.Serialization;

using UGSGit.PluginAbstractions;

namespace UGSGit.Models;

/// <summary>
/// Main app's JSON context extending PluginAbstractionsJsonContext.
/// Registers types for AOT compatibility when DISABLE_PLUGINS is not set.
/// </summary>
[JsonSerializable(typeof(UgsConfig))]
[JsonSerializable(typeof(UgsWorkspaceState))]
[JsonSerializable(typeof(UProjectMeta))]
[JsonSerializable(typeof(UgsBuildStep))]
[JsonSerializable(typeof(BuildVersion))]
[JsonSerializable(typeof(EngineInfo))]
[JsonSerializable(typeof(PublishManifest))]
[JsonSerializable(typeof(BuildResult))]
[JsonSerializable(typeof(UatCommandPreset))]
internal partial class UnrealSyncJsonContext : PluginAbstractionsJsonContext
{
}
