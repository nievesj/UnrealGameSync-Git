#nullable enable

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Build step definition that aligns with the buildTargets config in .unrealsync-settings.json.
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
/// <param name="Id">Unique identifier for this build step.</param>
/// <param name="DisplayName">Human-readable name shown in the UI for this build step.</param>
/// <param name="Target">Build target name (e.g. "UnrealEditor", "Game").</param>
/// <param name="Platform">Target platform (e.g. "Win64", "Android").</param>
/// <param name="Configuration">Build configuration (e.g. "Development", "Shipping").</param>
/// <param name="ScriptPath">Path to build script. Supports {EnginePath} and {ProjectName} template variables.</param>
/// <param name="Arguments">Command-line arguments passed to the script. Supports template variable substitution.</param>
/// <param name="OrderIndex">Relative execution order among build steps (lower runs first).</param>
/// <param name="RunOnNormalSync">Whether to execute this step during a normal (user-initiated) sync.</param>
/// <param name="RunOnScheduledSync">Whether to execute this step during a scheduled/automated sync.</param>
/// <param name="BuildMode">Build tool mode: "Ubt" (Unreal Build Tool), "Uat" (Unreal Automation Tool), or "Custom". Determines default argument format.</param>
/// <param name="UatCommand">UAT command preset ID when BuildMode is "Uat" (e.g. "BuildCookRun"). Null means no preset.</param>
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