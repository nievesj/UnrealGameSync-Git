using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace UGSGit.Models;

/// <summary>
/// Parsed .uproject file metadata. Uses a tolerant JSON parser that handles
/// UE's non-standard JSON (C-style comments, trailing commas).
/// </summary>
public class UProjectMeta
{
    [JsonPropertyName("FileVersion")]
    public int FileVersion { get; init; }

    [JsonPropertyName("EngineAssociation")]
    public string EngineAssociation { get; init; } = string.Empty;

    [JsonPropertyName("Category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("Modules")]
    public List<UProjectModule> Modules { get; init; } = new();

    [JsonPropertyName("Plugins")]
    public List<UProjectPlugin> Plugins { get; init; } = new();

    private static readonly JsonSerializerOptions UProjectOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Parse a .uproject file with UE's non-standard JSON (comments, trailing commas). Fixes BF-6.
    /// Uses System.Text.Json native comment support (fixes M-1: regex corrupted URLs).
    /// </summary>
    public static UProjectMeta ParseTolerant(string json)
    {
        // System.Text.Json natively skips // and /* */ comments with ReadCommentHandling.Skip.
        // UE also uses trailing commas, which STJ doesn't support — strip them.
        json = Regex.Replace(json, @",(\s*[}\]])", "$1");

        return JsonSerializer.Deserialize<UProjectMeta>(json, UProjectOptions)
            ?? throw new InvalidOperationException("Failed to parse .uproject file");
    }
}

public class UProjectModule
{
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("LoadingPhase")]
    public string LoadingPhase { get; init; } = string.Empty;
}

public class UProjectPlugin
{
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("Enabled")]
    public bool Enabled { get; init; }
}
