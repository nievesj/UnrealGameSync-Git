using System;
using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Sidecar manifest written alongside published zip files.
/// Contains metadata about the published build for downstream consumers.
/// </summary>
public class PublishManifest
{
    /// <summary>File name of the published zip archive.</summary>
    [JsonPropertyName("zipName")] public string ZipName { get; init; } = string.Empty;

    /// <summary>Target channel for the publish, e.g. "live", "staging", or "test".</summary>
    [JsonPropertyName("channel")] public string Channel { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the publish was created.</summary>
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; init; }

    /// <summary>Total size of the published zip file in bytes.</summary>
    [JsonPropertyName("fileSize")] public long FileSize { get; init; }
}
