using System.Collections.Generic;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.HelloWorld
{
    /// <summary>
    /// Creates a HelloWorld tab with context-based TabId.
    /// </summary>
    public class HelloWorldPluginManifest : IPluginManifest
    {
        public string PluginId => "sourcegit.hello-world";
        public string DisplayName => "Hello World";
        public string Description => "Reference plugin demonstrating the tab system";
        public string Version => "1.0.0";
        public string Author => "UGSGit";
        public bool IsGlobalByDefault => false;
        public int DefaultSortOrder => 500;

        public IReadOnlyList<IRepositoryTab> CreateTabs(PluginContext context)
        {
            return new List<IRepositoryTab>
            {
                new HelloWorldTab(context.RepositoryPath)
            };
        }

        /// <summary>
        /// HelloWorld plugin does not contribute commit annotations.
        /// </summary>
        public IReadOnlyList<ICommitAnnotator>? CommitAnnotators => null;
    }
}
