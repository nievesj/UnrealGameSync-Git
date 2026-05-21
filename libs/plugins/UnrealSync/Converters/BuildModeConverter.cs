using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace UGSGit.Plugins.UnrealSync.Converters;

/// <summary>
/// Converts between BuildMode string ("Ubt"/"Uat"/"Custom") and ComboBox SelectedIndex (0/1/2).
/// </summary>
public class BuildModeConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use as a static resource in AXAML bindings.
    /// </summary>
    public static readonly BuildModeConverter Instance = new();

    private static readonly string[] Modes = { "Ubt", "Uat", "Custom" };

    /// <summary>
    /// Converts a BuildMode string to the corresponding ComboBox SelectedIndex.
    /// </summary>
    /// <param name="value">The BuildMode string value ("Ubt", "Uat", "Custom", or null).</param>
    /// <param name="targetType">The target binding type. Not used by this converter.</param>
    /// <param name="parameter">An optional converter parameter. Not used by this converter.</param>
    /// <param name="culture">The culture for locale-aware conversion. Not used by this converter.</param>
    /// <returns>The zero-based index matching the mode string, or 0 if the value is unrecognized.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string mode)
        {
            var idx = Array.IndexOf(Modes, mode);
            return idx >= 0 ? idx : 0;
        }
        return 0;
    }

    /// <summary>
    /// Converts a ComboBox SelectedIndex back to the corresponding BuildMode string.
    /// </summary>
    /// <param name="value">The ComboBox SelectedIndex integer value.</param>
    /// <param name="targetType">The target binding type. Not used by this converter.</param>
    /// <param name="parameter">An optional converter parameter. Not used by this converter.</param>
    /// <param name="culture">The culture for locale-aware conversion. Not used by this converter.</param>
    /// <returns>The mode string matching the index, or "Ubt" if the index is out of range.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int idx && idx >= 0 && idx < Modes.Length)
            return Modes[idx];
        return "Ubt";
    }
}
