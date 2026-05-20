using System;
using System.Collections.Generic;
using System.Linq;

namespace UGSGit.PluginAbstractions
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

        /// <summary>
        /// Resolve a registered service by its interface type.
        /// Throws InvalidOperationException if not registered.
        /// </summary>
        public T GetRequiredService<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service)
                ? (T)service
                : throw new InvalidOperationException(
                    $"Service of type {typeof(T).Name} is not registered. " +
                    "Ensure PluginActivator has registered all required services before calling CreateTabs().");
        }

        /// <summary>
        /// Returns the set of service types that must always be registered before CreateTabs().
        /// </summary>
        private static readonly Type[] RequiredServices =
        {
            typeof(IPluginLogger),
            typeof(IGitSyncService),
            typeof(IConfigService),
        };

        /// <summary>
        /// Validates that all required services are registered.
        /// Called by PluginActivator after registration, before CreateTabs().
        /// Throws InvalidOperationException listing missing services.
        /// </summary>
        public void ValidateRequiredServices()
        {
            var missing = RequiredServices
                .Where(t => !_services.ContainsKey(t))
                .Select(t => t.Name)
                .ToList();

            if (missing.Count > 0)
                throw new InvalidOperationException(
                    $"Missing required plugin services: {string.Join(", ", missing)}. " +
                    "Ensure PluginActivator registers these before calling CreateTabs().");
        }
    }
}
