using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace UGSGit.Plugins.UnrealSync.Converters;

/// <summary>
/// Provides context-aware watermark text for the ScriptPath TextBox based on BuildMode.
/// </summary>
public class BuildModeScriptPathHintConverter : IValueConverter
{
    public static readonly BuildModeScriptPathHintConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string mode)
        {
            return mode switch
            {
                "Ubt" => "e.g. {EnginePath}/Engine/Build/BatchFiles/Build.bat",
                "Uat" => "e.g. {EnginePath}/Engine/Build/BatchFiles/RunUAT.bat",
                "Custom" => "Path to your custom build script",
                _ => "Script path"
            };
        }
        return "Script path";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
