using System;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Logger abstraction for plugins. Host implementation wraps Native.OS.Log/LogException.
/// This service is guaranteed available to all plugins (both built-in and external).
/// </summary>
public interface IPluginLogger
{
    /// <summary>Writes an informational log message.</summary>
    /// <param name="message">The message to log.</param>
    void Log(string message);

    /// <summary>Writes an error log message with an associated exception.</summary>
    /// <param name="message">Description of the error context.</param>
    /// <param name="ex">The exception that caused the error.</param>
    void LogError(string message, Exception ex);
}
