#nullable enable

namespace UGSGit.PluginAbstractions;

/// <summary>
/// A package profile defining what to build and stage via BuildGraph.
/// EditorTarget is derived from the .uproject filename.
/// </summary>
/// <param name="Id">Unique identifier for this package profile.</param>
/// <param name="DisplayName">Human-readable name shown in the UI for this profile.</param>
/// <param name="EditorTarget">Editor target name, typically derived from the .uproject filename without extension.</param>
/// <param name="Platform">Target platform for packaging (e.g. "Win64", "Android").</param>
/// <param name="Configuration">Build configuration for packaging (e.g. "Development", "Shipping").</param>
/// <param name="IncludePdb">Whether to include PDB/symbol files in the packaged output.</param>
/// <param name="BuildGraphScript">Per-profile BuildGraph script path override; null falls back to UgsConfig.BuildGraph.</param>
/// <param name="BuildGraphTarget">Per-profile BuildGraph target override; null falls back to UgsConfig.BuildGraph.</param>
public record UgsPackageProfile(
    string Id,
    string DisplayName,
    string EditorTarget,
    string Platform,
    string Configuration,
    bool IncludePdb = false,
    string? BuildGraphScript = null,
    string? BuildGraphTarget = null
);
