using System.Text.Json.Serialization;

namespace UGSGit.Models;

/// <summary>
/// Source-generated JSON context for all UnrealSync types.
/// Required for AOT compatibility when DISABLE_PLUGINS is not set.
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
internal partial class UnrealSyncJsonContext : JsonSerializerContext { }
