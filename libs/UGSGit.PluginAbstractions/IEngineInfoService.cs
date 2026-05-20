namespace UGSGit.PluginAbstractions;

/// <summary>
/// Engine info detection abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.EngineInfoService.
/// </summary>
public interface IEngineInfoService
{
    EngineInfo Detect(string enginePath);
}
