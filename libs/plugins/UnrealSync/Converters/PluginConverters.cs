using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace UGSGit.Plugins.UnrealSync.Converters
{
    /// <summary>
    /// Minimal string converters for plugin AXAML files, avoiding dependency on
    /// the main project's UGSGit.Converters namespace (which would create a circular dependency).
    /// </summary>
    public static class StringConverters
    {
        /// <summary>
        /// Returns true if the string value is not null or empty.
        /// </summary>
        public static readonly FuncValueConverter<string?, bool> IsNotNullOrEmpty =
            new FuncValueConverter<string?, bool>(v => !string.IsNullOrEmpty(v));
    }

    /// <summary>
    /// Minimal object converters for plugin AXAML files, avoiding dependency on
    /// the main project's UGSGit.Converters namespace (which would create a circular dependency).
    /// </summary>
    public static class ObjectConverters
    {
        /// <summary>
        /// Returns true if the value is not null.
        /// </summary>
        public static readonly FuncValueConverter<object?, bool> IsNotNull =
            new FuncValueConverter<object?, bool>(v => v != null);
    }

    /// <summary>
    /// Converts a hex color string to a SolidColorBrush.
    /// Returns the ConverterParameter (or Transparent brush) if empty/invalid.
    /// </summary>
    public class HexToBrushConverter : IValueConverter
    {
        public static readonly HexToBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            Color defaultColor = Colors.Transparent;
            if (parameter is string paramStr && !string.IsNullOrWhiteSpace(paramStr))
            {
                try { defaultColor = Color.Parse(paramStr); }
                catch (FormatException) { /* invalid default color — fall through */ }
            }

            var v = value as string;
            if (string.IsNullOrWhiteSpace(v))
                return new SolidColorBrush(defaultColor);

            var hex = v.Trim();
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            try
            {
                return new SolidColorBrush(Color.Parse(hex));
            }
            catch (FormatException)
            {
                return new SolidColorBrush(defaultColor);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
