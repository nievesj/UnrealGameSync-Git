using System;
using System.Collections.Generic;
using System.IO;

using UGSGit.Models;
using UGSGit.Services;
using UGSGit.ViewModels.Tabs;

namespace UGSGit.ViewModels
{
    /// <summary>
    /// Creates tab instances from enabled plugins for a specific repository.
    /// Called by LauncherPage after RegisterBuiltInTabs().
    /// 
    /// Moved from Models namespace to ViewModels to break the circular dependency.
    /// </summary>
    public static class PluginActivator
    {
        /// <summary>
        /// Creates and adds tabs for all enabled plugins for the given repository.
        /// Called during LauncherPage initialization.
        /// </summary>
        public static void ActivateEnabledPlugins(LauncherPage page, RepositoryNode node, RepositoryUIStates uiStates)
        {
            foreach (var manifest in PluginRegistry.Instance.AllManifests)
            {
                if (!PluginRegistry.Instance.IsEnabledForRepository(manifest.PluginId, uiStates))
                    continue;

                ActivatePlugin(manifest, page, node);
            }
        }

        /// <summary>
        /// Activates a single plugin (tab creation and addition).
        /// </summary>
        public static void ActivatePlugin(IPluginManifest manifest, LauncherPage page, RepositoryNode node)
        {
            var repoPath = node.Id;
            var isFirstLoad = !PluginRegistry.Instance.HasBeenActivated(manifest.PluginId, repoPath);

            // Register services so plugins can resolve them via context.GetService<T>()
            var context = new PluginContext
            {
                RepositoryPath = repoPath,
                RepositoryName = node.Name,
                GitDirectory = Path.Combine(repoPath, ".git"),
                IsFirstLoadForRepository = isFirstLoad,
            };

            context.RegisterService<IGitSyncService>(new GitSyncService(repoPath));
            context.RegisterService<IConfigService>(new ConfigServiceAdapter());
            context.RegisterService<IEngineDetector>(new EngineDetectorAdapter());
            context.RegisterService<IEngineInfoService>(new EngineInfoServiceAdapter());

            // Factories for services that need runtime parameters (enginePath, uprojectPath)
            context.RegisterService<Func<string, string, IBuildService>>(
                (enginePath, uprojectPath) => new BuildService(repoPath, enginePath, uprojectPath));
            context.RegisterService<Func<string, IEditorLauncher>>(
                (enginePath) => new EditorLauncher(enginePath));
            context.RegisterService<Func<string, string, UgsConfig, IBuildGraphService>>(
                (enginePath, rp, config) => new BuildGraphService(enginePath, rp, config));

            // Stateless services registered as singletons
            context.RegisterService<IPublishService>(new PublishService());

            IReadOnlyList<IRepositoryTab> tabs;
            try
            {
                tabs = manifest.CreateTabs(context);
            }
            catch (Exception ex)
            {
                Native.OS.LogException(new InvalidOperationException(
                    $"Plugin '{manifest.PluginId}' failed to create tabs: {ex.Message}", ex));

                var errorTab = new ErrorTab(
                    manifest.PluginId,
                    manifest.DisplayName,
                    new InvalidOperationException($"Plugin initialization failed: {ex.Message}", ex));
                page.AddPluginTab(errorTab);

                PluginRegistry.Instance.RegisterTabToPlugin(errorTab.TabId, manifest.PluginId);
                return;
            }

            if (tabs != null)
            {
                foreach (var tab in tabs)
                {
                    page.AddPluginTab(tab);
                    PluginRegistry.Instance.RegisterTabToPlugin(tab.TabId, manifest.PluginId);
                }
            }

            PluginRegistry.Instance.MarkActivated(manifest.PluginId, repoPath);
        }

        /// <summary>
        /// Deactivates a single plugin by removing its tabs from the page.
        /// </summary>
        public static void DeactivatePlugin(IPluginManifest manifest, LauncherPage page)
        {
            var tabIds = PluginRegistry.Instance.GetTabIdsForPlugin(manifest.PluginId);
            var tabsToRemove = new List<IRepositoryTab>();

            foreach (var descriptor in page.Tabs)
            {
                if (tabIds.Contains(descriptor.TabId))
                    tabsToRemove.Add(descriptor.Tab);
            }

            foreach (var tab in tabsToRemove)
            {
                PluginRegistry.Instance.UnregisterTabFromPlugin(tab.TabId, manifest.PluginId);
                page.RemovePluginTab(tab);
            }
        }
    }
}
