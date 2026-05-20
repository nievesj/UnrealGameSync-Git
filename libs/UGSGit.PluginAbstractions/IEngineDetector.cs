namespace UGSGit.Models;

/// <summary>
/// Engine detection abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.EngineDetector.
/// </summary>
public interface IEngineDetector
{
    string? Detect(UProjectMeta meta, string repoPath);
    void ClearCache();
}
