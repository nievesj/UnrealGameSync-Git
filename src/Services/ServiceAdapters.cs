#nullable enable

using UGSGit.Models;

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
