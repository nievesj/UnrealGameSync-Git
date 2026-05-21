#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Predefined UAT command preset. Each maps to a RunUAT.bat command
/// with its canonical argument format.
/// </summary>
/// <param name="Id">Unique identifier for this preset, e.g. "BuildCookRun" or "BuildEditor".</param>
/// <param name="DisplayName">Human-readable name shown in the UI, e.g. "BuildCookRun (Full Package)".</param>
/// <param name="ArgumentsTemplate">Template string with {variables} that gets expanded before execution.</param>
/// <param name="AutoBuildType">Auto-selected build type: "Game", "Editor", "Server", or "" to keep user choice.</param>
public record UatCommandPreset(
    string Id,               // "BuildCookRun", "BuildEditor", etc.
    string DisplayName,      // "BuildCookRun (Full Package)", etc.
    string ArgumentsTemplate, // Template string with {variables}
    string AutoBuildType      // "Game", "Editor", "Server", or "" (keep user choice)
);

/// <summary>
/// Built-in UAT command presets with exact argument templates derived from
/// Epic's official documentation and verified against the UAT source code.
/// </summary>
public static class UatCommandPresets
{
    /// <summary>List of all built-in UAT command presets.</summary>
    public static readonly List<UatCommandPreset> All = new()
    {
        new("BuildCookRun", "BuildCookRun (Full Package)",
            "BuildCookRun -project=\"{ProjectPath}\" -targetplatform={Platform} " +
            "-clientconfig={Configuration} -serverconfig={Configuration} " +
            "-noP4 -build -cook -allmaps -stage -pak -archive " +
            "-archivedirectory=\"{ArchiveDir}\"",
            ""),

        new("BuildEditor", "BuildEditor (Editor Only)",
            "BuildEditor -project=\"{ProjectPath}\" -notools",
            "Editor"),

        new("BuildGame", "BuildGame (Game Client)",
            "BuildGame -project=\"{ProjectPath}\" -platform={Platform} " +
            "-clientconfig={Configuration} -notools",
            "Game"),

        new("BuildServer", "BuildServer (Dedicated Server)",
            "BuildServer -project=\"{ProjectPath}\" -platform={Platform} " +
            "-serverconfig={Configuration}",
            "Server"),

        new("BuildGraph", "BuildGraph (Pipeline)",
            "BuildGraph " +
            "-script=\"Engine/Build/Graph/Examples/BuildEditorAndTools.xml\" " +
            "-target=\"Copy to Staging Directory\" " +
            "-set:EditorTarget={UbtTarget} -set:ArchiveStream={UbtTarget}",
            "Editor"),

        new("Custom", "Custom UAT Command", "", ""),
    };

    /// <summary>Looks up a preset by its unique identifier.</summary>
    /// <param name="id">The preset identifier to find. Returns null if null or empty.</param>
    /// <returns>The matching <see cref="UatCommandPreset"/>, or null if no preset with the given id exists.</returns>
    public static UatCommandPreset? Find(string? id) =>
        string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(p => p.Id == id);
}