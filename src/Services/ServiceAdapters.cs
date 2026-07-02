#nullable enable

using System;
using System.IO;

using UGSGit.PluginAbstractions;

namespace UGSGit.Services;

/// <summary>
/// Adapter that wraps the static ConfigService to implement IConfigService.
/// </summary>
public class ConfigServiceAdapter : IConfigService
{
    public UgsConfig LoadConfig(string repoPath)
    {
        return ConfigService.LoadConfig(repoPath);
    }

    public void SaveConfig(string repoPath, UgsConfig config)
    {
        ConfigService.SaveConfig(repoPath, config);
    }

    public UgsWorkspaceState LoadLocalState(string repoPath)
    {
        return ConfigService.LoadLocalState(repoPath);
    }

    public void SaveLocalState(string repoPath, UgsWorkspaceState state)
    {
        ConfigService.SaveLocalState(repoPath, state);
    }
}

/// <summary>
/// Adapter that wraps the static EngineDetector to implement IEngineDetector.
/// </summary>
public class EngineDetectorAdapter : IEngineDetector
{
    public string? Detect(UProjectMeta meta, string repoPath)
    {
        return EngineDetector.Detect(meta, repoPath);
    }

    public void ClearCache()
    {
        EngineDetector.ClearCache();
    }
}

/// <summary>
/// Adapter that wraps the static EngineInfoService to implement IEngineInfoService.
/// </summary>
public class EngineInfoServiceAdapter : IEngineInfoService
{
    public EngineInfo Detect(string enginePath)
    {
        return EngineInfoService.Detect(enginePath);
    }
}

/// <summary>
/// Adapter that wraps Native.OS logging into IPluginLogger for plugins.
/// Writes informational logs to a plugin log file under the data directory.
/// Error logs write the message to the log file then delegate to Native.OS.LogException.
/// </summary>
public class PluginLoggerAdapter : IPluginLogger
{
    private static readonly object s_logLock = new();
    private readonly string _logDir;
    private readonly string _logPath;

    public PluginLoggerAdapter()
    {
        _logDir = Path.Combine(Native.OS.DataDir, "plugins");
        Directory.CreateDirectory(_logDir);
        _logPath = Path.Combine(_logDir, "plugin.log");
    }

    public void Log(string message)
    {
        lock (s_logLock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(_logPath, $"[{timestamp}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Best-effort logging — don't crash the app
            }
        }
    }

    public void LogError(string message, Exception ex)
    {
        // Log the context message first
        Log($"ERROR: {message}");

        // Then delegate to crash-file logging for the full exception
        Native.OS.LogException(ex);
    }
}

/// <summary>
/// Factory adapter that creates IBuildService instances.
/// Captures repoPath at construction time (from PluginActivator).
/// </summary>
public class BuildServiceFactory : IBuildServiceFactory
{
    private readonly string _repoPath;

    public BuildServiceFactory(string repoPath) => _repoPath = repoPath;

    public IBuildService Create(string enginePath, string uprojectPath)
        => new BuildService(_repoPath, enginePath, uprojectPath);
}

/// <summary>
/// Factory adapter that creates IBuildGraphService instances.
/// Captures repoPath at construction time (from PluginActivator).
/// </summary>
public class BuildGraphServiceFactory : IBuildGraphServiceFactory
{
    private readonly string _repoPath;

    public BuildGraphServiceFactory(string repoPath) => _repoPath = repoPath;

    public IBuildGraphService Create(string enginePath, UgsConfig config)
        => new BuildGraphService(enginePath, _repoPath, config);

    public IBuildGraphService Create(string enginePath, UgsConfig config, string uprojectPath, string shortSha, string projectName)
        => new BuildGraphService(enginePath, _repoPath, config, uprojectPath, shortSha, projectName);
}

/// <summary>
/// Factory adapter that creates IEditorLauncher instances.
/// </summary>
public class EditorLauncherFactory : IEditorLauncherFactory
{
    public IEditorLauncher Create(string enginePath)
        => new EditorLauncher(enginePath);
}
