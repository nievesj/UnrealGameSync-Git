using System.Collections.Generic;

using UGSGit.Models;

namespace UGSGit.ViewModels.Tabs.UnrealSync;

public class UnrealSyncManifest : IPluginManifest
{
    public string PluginId => "sourcegit.unrealsync";
    public string DisplayName => "UnrealSync";
    public string Description => "Unreal Engine workspace sync, build, and launch workflow for Git";
    public string Version => "0.1.0";
    public string Author => "UGSGit";
    public bool IsGlobalByDefault => true;
    public int DefaultSortOrder => 100;

    public IReadOnlyList<IRepositoryTab> CreateTabs(PluginContext context)
    {
        var tab = new UnrealSyncTab(context.RepositoryPath);
        return new List<IRepositoryTab> { tab };
    }
}
