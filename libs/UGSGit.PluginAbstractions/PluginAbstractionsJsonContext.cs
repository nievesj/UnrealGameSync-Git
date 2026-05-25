using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Source-generated JSON serialization context for all types defined in PluginAbstractions.
/// External plugins reference this via NuGet package to serialize shared types with AOT compatibility.
/// This is the base context that <c>UnrealSyncJsonContext</c> extends by referencing types from the UnrealSync plugin.
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
[JsonSerializable(typeof(UgsBuildConfig))]
[JsonSerializable(typeof(UgsPackageProfile))]
[JsonSerializable(typeof(StageResult))]
[JsonSerializable(typeof(SyncResult))]
[JsonSerializable(typeof(PublishProgress))]
[JsonSerializable(typeof(PublishResult))]
[JsonSerializable(typeof(DeployManifest))]
[JsonSerializable(typeof(DeployManifestFile))]
[JsonSerializable(typeof(DeployBuildInfo))]
[JsonSerializable(typeof(DeployResult))]
[JsonSerializable(typeof(DeployStatus))]
[JsonSerializable(typeof(CommitAnnotation))]
[JsonSerializable(typeof(CommitRef))]
[JsonSerializable(typeof(UgsChangeTypeConfig))]
public partial class PluginAbstractionsJsonContext : JsonSerializerContext
{
}
