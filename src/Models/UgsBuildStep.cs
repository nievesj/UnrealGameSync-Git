namespace SourceGit.Models;

/// <summary>
/// Build step definition that aligns with the buildTargets config in .unrealsync.json.
/// Maps 1:1 to config fields per RC-5 fix.
/// </summary>
public record UgsBuildStep(
    string Id,
    string DisplayName,
    string Target,
    string Platform,
    string Configuration,
    string ScriptPath,        // empty = UBT target, non-empty = custom script path
    int OrderIndex,
    bool RunOnNormalSync,
    bool RunOnScheduledSync
);
