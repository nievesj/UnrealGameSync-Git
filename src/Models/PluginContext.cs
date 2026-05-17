namespace UGSGit.Models
{
    /// <summary>
    /// Provides plugins with runtime information without giving them direct access to the app's ViewModels.
    /// </summary>
    public class PluginContext
    {
        /// <summary>Normalized repository path (e.g. "D:/Projects/MyGame")</summary>
        public string RepositoryPath { get; init; }

        /// <summary>Repository display name</summary>
        public string RepositoryName { get; init; }

        /// <summary>Path to the .git directory</summary>
        public string GitDirectory { get; init; }

        /// <summary>Whether this is the first time the plugin is loaded for this repository</summary>
        public bool IsFirstLoadForRepository { get; init; }
    }
}