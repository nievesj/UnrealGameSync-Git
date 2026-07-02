using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Parsed .uproject file metadata. Uses a tolerant JSON parser that handles
/// UE's non-standard JSON (C-style comments, trailing commas).
/// </summary>
public class UProjectMeta
{
    /// <summary>File format version number from the .uproject file.</summary>
    [JsonPropertyName("FileVersion")]
    public int FileVersion { get; init; }

    /// <summary>Engine association string used to locate the Unreal Engine installation (e.g. "4.27", "5.3").</summary>
    [JsonPropertyName("EngineAssociation")]
    public string EngineAssociation { get; init; } = string.Empty;

    /// <summary>Project category for organizational purposes (e.g. "Games", "Film / Video").</summary>
    [JsonPropertyName("Category")]
    public string Category { get; init; } = string.Empty;

    /// <summary>Project description text.</summary>
    [JsonPropertyName("Description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>List of modules defined in the project.</summary>
    [JsonPropertyName("Modules")]
    public List<UProjectModule> Modules { get; init; } = new();

    /// <summary>List of plugins referenced or embedded in the project.</summary>
    [JsonPropertyName("Plugins")]
    public List<UProjectPlugin> Plugins { get; init; } = new();

    private static readonly JsonSerializerOptions UProjectOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolverChain = { PluginAbstractionsJsonContext.Default }
    };

    /// <summary>
    /// Parse a .uproject file with UE's non-standard JSON (comments, trailing commas). Fixes BF-6.
    /// Uses System.Text.Json native comment support (fixes M-1: regex corrupted URLs).
    /// </summary>
    /// <param name="json">Raw JSON content from a .uproject file.</param>
    /// <returns>A populated <see cref="UProjectMeta"/> instance. Throws <see cref="InvalidOperationException"/> if deserialization fails.</returns>
    public static UProjectMeta ParseTolerant(string json)
    {
        // System.Text.Json natively skips // and /* */ comments with ReadCommentHandling.Skip.
        // UE also uses trailing commas, which STJ doesn't support — strip them.
        json = Regex.Replace(json, @",(\s*[}\]])", "$1");

        return JsonSerializer.Deserialize<UProjectMeta>(json, UProjectOptions)
            ?? throw new InvalidOperationException("Failed to parse .uproject file");
    }
}

/// <summary>
/// A module entry in the .uproject file.
/// </summary>
public class UProjectModule
{
    /// <summary>Module name as declared in the .uproject file.</summary>
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Module type (e.g. "Runtime", "Developer", "Editor").</summary>
    [JsonPropertyName("Type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>Loading phase that determines when this module is loaded (e.g. "Default", "PreDefault", "PostConfigInit").</summary>
    [JsonPropertyName("LoadingPhase")]
    public string LoadingPhase { get; init; } = string.Empty;
}

/// <summary>
/// A plugin entry in the .uproject file.
/// </summary>
public class UProjectPlugin
{
    /// <summary>Plugin name as declared in the .uproject file.</summary>
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Whether this plugin is enabled for the project.</summary>
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; init; }
}
