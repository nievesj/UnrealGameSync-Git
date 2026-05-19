#nullable enable

namespace SourceGit.Models;

/// <summary>
/// Build step definition that aligns with the buildTargets config in .unrealsync.json.
/// Maps 1:1 to config fields per RC-5 fix.
///
/// <para>BuildMode determines which UE build tool is invoked:</para>
/// <para>- "Ubt" = Unreal Build Tool (Build.bat) — compiles C++ only</para>
/// <para>- "Uat" = Unreal Automation Tool (RunUAT.bat) — cook/package/stage/deploy</para>
/// <para>- "Custom" = User-provided script with no pre-populated arguments</para>
///
/// <para>UatCommand selects the UAT command preset when BuildMode is "Uat".</para>
/// <para>Both trailing params have defaults for backward compatibility with old configs.</para>
/// </summary>
public record UgsBuildStep(
    string Id,
    string DisplayName,
    string Target,
    string Platform,
    string Configuration,
    string ScriptPath,        // path to build script (supports {EnginePath}, {ProjectName} variables)
    string Arguments,         // command-line args passed to the script (supports template vars)
    int OrderIndex,
    bool RunOnNormalSync,
    bool RunOnScheduledSync,
    string BuildMode = "Ubt",       // "Ubt", "Uat", or "Custom" — determines default argument format
    string? UatCommand = null       // UAT command preset ID (e.g. "BuildCookRun"), null = no preset
);