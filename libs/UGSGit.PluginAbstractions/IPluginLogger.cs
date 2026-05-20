using System;

namespace UGSGit.Models;

/// <summary>
/// Logger abstraction for plugins. Host implementation wraps Native.OS.Log/LogException.
/// </summary>
public interface IPluginLogger
{
    void Log(string message);
    void LogError(string message, Exception ex);
}
