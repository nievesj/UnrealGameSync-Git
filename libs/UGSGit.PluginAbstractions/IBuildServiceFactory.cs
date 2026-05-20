namespace UGSGit.PluginAbstractions;

/// <summary>
/// Factory for creating IBuildService instances with runtime parameters.
/// Host implementation injects repo path at construction time.
/// </summary>
public interface IBuildServiceFactory
{
    IBuildService Create(string enginePath, string uprojectPath);
}
