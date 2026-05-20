using System;
using System.Collections.Generic;

namespace UGSGit.Models
{
    /// <summary>
    /// Provides plugins with runtime information without giving them direct access to the app's ViewModels.
    /// </summary>
    public class PluginContext
    {
        private readonly Dictionary<Type, object> _services = new();

        /// <summary>Normalized repository path (e.g. "D:/Projects/MyGame")</summary>
        public string RepositoryPath { get; init; }

        /// <summary>Repository display name</summary>
        public string RepositoryName { get; init; }

        /// <summary>Path to the .git directory</summary>
        public string GitDirectory { get; init; }

        /// <summary>Whether this is the first time the plugin is loaded for this repository</summary>
        public bool IsFirstLoadForRepository { get; init; }

        /// <summary>
        /// Register a service instance by its interface type.
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Resolve a registered service by its interface type.
        /// Returns null if not registered.
        /// </summary>
        public T? GetService<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
        }
    }
}
