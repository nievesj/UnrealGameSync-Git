namespace UGSGit.Models;

/// <summary>
/// A package profile defining what to build and stage via BuildGraph.
/// EditorTarget is derived from the .uproject filename.
/// </summary>
public record UgsPackageProfile(
    string Id,
    string DisplayName,
    string EditorTarget,
    string Platform,
    string Configuration,
    bool IncludePdb = false
);
