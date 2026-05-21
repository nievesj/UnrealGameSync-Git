using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace UGSGit.Plugins.UnrealSync.Converters;

/// <summary>
/// Provides context-aware watermark text for the ScriptPath TextBox based on BuildMode.
/// </summary>
public class BuildModeScriptPathHintConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use as a static resource in AXAML bindings.
    /// </summary>
    public static readonly BuildModeScriptPathHintConverter Instance = new();

    /// <summary>
    /// Converts a BuildMode string to a contextual watermark hint for the ScriptPath TextBox.
    /// </summary>
    /// <param name="value">The BuildMode string value ("Ubt", "Uat", "Custom", or null).</param>
    /// <param name="targetType">The target binding type. Not used by this converter.</param>
    /// <param name="parameter">An optional converter parameter. Not used by this converter.</param>
    /// <param name="culture">The culture for locale-aware conversion. Not used by this converter.</param>
    /// <returns>A watermark hint string appropriate for the given build mode, or "Script path" as fallback.</returns>
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

    /// <summary>
    /// ConvertBack is not supported for this converter; always returns null.
    /// </summary>
    /// <param name="value">The target value to convert back. Not used by this converter.</param>
    /// <param name="targetType">The target binding type. Not used by this converter.</param>
    /// <param name="parameter">An optional converter parameter. Not used by this converter.</param>
    /// <param name="culture">The culture for locale-aware conversion. Not used by this converter.</param>
    /// <returns>Always null, as one-way conversion is not supported.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
