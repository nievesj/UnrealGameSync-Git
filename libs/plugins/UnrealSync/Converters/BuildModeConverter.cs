using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace UGSGit.Plugins.UnrealSync.Converters;

/// <summary>
/// Converts between BuildMode string ("Ubt"/"Uat"/"Custom") and ComboBox SelectedIndex (0/1/2).
/// </summary>
public class BuildModeConverter : IValueConverter
{
    public static readonly BuildModeConverter Instance = new();

    private static readonly string[] Modes = { "Ubt", "Uat", "Custom" };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string mode)
        {
            var idx = Array.IndexOf(Modes, mode);
            return idx >= 0 ? idx : 0;
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int idx && idx >= 0 && idx < Modes.Length)
            return Modes[idx];
        return "Ubt";
    }
}
