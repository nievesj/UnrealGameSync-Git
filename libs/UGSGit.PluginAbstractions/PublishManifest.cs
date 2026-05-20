using System;
using System.Text.Json.Serialization;

namespace UGSGit.Models;

/// <summary>
/// Sidecar manifest written alongside published zip files.
/// Contains metadata about the published build for downstream consumers.
/// </summary>
public class PublishManifest
{
    [JsonPropertyName("zipName")] public string ZipName { get; init; } = string.Empty;
    [JsonPropertyName("channel")] public string Channel { get; init; } = string.Empty;
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; init; }
    [JsonPropertyName("fileSize")] public long FileSize { get; init; }
}
