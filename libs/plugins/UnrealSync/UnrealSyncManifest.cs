using System.Collections.Generic;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Plugin manifest declaring metadata and tab factory for UnrealSync.
/// </summary>
public class UnrealSyncManifest : IPluginManifest
{
    /// <summary>
    /// Unique plugin identifier string.
    /// </summary>
    public string PluginId => "sourcegit.unrealsync";

    /// <summary>
    /// Human-readable plugin name shown in the UI.
    /// </summary>
    public string DisplayName => "UnrealSync";

    /// <summary>
    /// Brief description of what the plugin provides.
    /// </summary>
    public string Description => "Unreal Engine workspace sync, build, and launch workflow for Git";

    /// <summary>
    /// Plugin version string in semver format.
    /// </summary>
    public string Version => "0.1.0";

    /// <summary>
    /// Name of the plugin author or maintainer.
    /// </summary>
    public string Author => "UGSGit";

    /// <summary>
    /// Whether this plugin is enabled by default for all repositories.
    /// </summary>
    public bool IsGlobalByDefault => true;

    /// <summary>
    /// Default sort order for the plugin's tabs in the tab bar.
    /// </summary>
    public int DefaultSortOrder => 100;

    /// <summary>
    /// Creates the repository tab instances for this plugin.
    /// </summary>
    /// <param name="context">Plugin context providing repository path and service resolution.</param>
    /// <returns>Read-only list of <see cref="IRepositoryTab"/> instances contributed by this plugin.</returns>
    public IReadOnlyList<IRepositoryTab> CreateTabs(PluginContext context)
    {
        var tab = new UnrealSyncTab(context);
        return new List<IRepositoryTab> { tab };
    }
}
