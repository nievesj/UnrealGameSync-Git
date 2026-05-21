namespace UGSGit.PluginAbstractions;

/// <summary>
/// Engine info detection abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.EngineInfoService.
/// </summary>
public interface IEngineInfoService
{
    /// <summary>
    /// Detects engine version information from the installation at the specified path.
    /// </summary>
    /// <param name="enginePath">Absolute path to the engine root directory.</param>
    /// <returns>An <see cref="EngineInfo"/> describing the engine version and label, or <c>null</c> if detection fails.</returns>
    EngineInfo Detect(string enginePath);
}
