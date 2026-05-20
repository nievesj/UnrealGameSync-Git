using System;

using Avalonia.Data.Converters;

namespace UGSGit.Plugins.Converters
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

    public static class ObjectConverters
    {
        /// <summary>
        /// Returns true if the value is not null.
        /// </summary>
        public static readonly FuncValueConverter<object?, bool> IsNotNull =
            new FuncValueConverter<object?, bool>(v => v != null);
    }
}
