namespace UGSGit.PluginAbstractions;

/// <summary>
/// Factory for creating IBuildGraphService instances with runtime parameters.
/// Host implementation injects repo path at construction time.
/// repoPath is not exposed in Create() since it is derivable from PluginContext.RepositoryPath.
/// </summary>
public interface IBuildGraphServiceFactory
{
    IBuildGraphService Create(string enginePath, UgsConfig config);
}
