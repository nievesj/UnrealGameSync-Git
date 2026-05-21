namespace UGSGit.PluginAbstractions;

/// <summary>
/// Config persistence abstraction for plugins.
/// Host implementation delegates to UGSGit.Services.ConfigService.
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Loads the team-shared UGS configuration from the repository's <c>.unrealsync.json</c> file.
    /// </summary>
    /// <param name="repoPath">Absolute path to the repository root.</param>
    /// <returns>The deserialized configuration, or a default <see cref="UgsConfig"/> if the file does not exist.</returns>
    UgsConfig LoadConfig(string repoPath);

    /// <summary>
    /// Saves the team-shared UGS configuration to the repository's <c>.unrealsync.json</c> file.
    /// </summary>
    /// <param name="repoPath">Absolute path to the repository root.</param>
    /// <param name="config">The configuration to persist.</param>
    void SaveConfig(string repoPath, UgsConfig config);

    /// <summary>
    /// Loads the user-local workspace state for the given repository.
    /// </summary>
    /// <param name="repoPath">Absolute path to the repository root.</param>
    /// <returns>The deserialized workspace state, or a default <see cref="UgsWorkspaceState"/> if none exists.</returns>
    UgsWorkspaceState LoadLocalState(string repoPath);

    /// <summary>
    /// Saves the user-local workspace state for the given repository.
    /// </summary>
    /// <param name="repoPath">Absolute path to the repository root.</param>
    /// <param name="state">The workspace state to persist.</param>
    void SaveLocalState(string repoPath, UgsWorkspaceState state);
}
