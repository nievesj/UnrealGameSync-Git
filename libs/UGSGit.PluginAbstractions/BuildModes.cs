namespace UGSGit.PluginAbstractions;

/// <summary>
/// Build mode constants. Stored as strings in JSON for forward-compatibility.
/// Determines which UE build tool is invoked and how arguments are formatted.
///
/// Ubt = Unreal Build Tool (Build.bat/Build.sh) — compiles C++ code only.
/// Uat = Unreal Automation Tool (RunUAT.bat/RunUAT.sh) — cook, package, stage, deploy.
/// Custom = User-provided script with no pre-populated arguments.
/// </summary>
public static class BuildModes
{
    public const string Ubt = "Ubt";
    public const string Uat = "Uat";
    public const string Custom = "Custom";
}