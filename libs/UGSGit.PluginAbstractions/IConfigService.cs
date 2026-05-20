namespace UGSGit.Models;

/// <summary>
/// Config persistence abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.ConfigService.
/// </summary>
public interface IConfigService
{
    UgsConfig LoadConfig(string repoPath);
    void SaveConfig(string repoPath, UgsConfig config);
    UgsWorkspaceState LoadLocalState(string repoPath);
    void SaveLocalState(string repoPath, UgsWorkspaceState state);
}
