using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace UGSGit.Models
{
    /// <summary>
    /// Scans the plugins/ directory for .NET DLLs, loads assemblies in an isolated
    /// AssemblyLoadContext (avoiding file locks and allowing future unload),
    /// and extracts IPluginManifest implementations.
    /// 
    /// NOTE: The plugin system is mutually exclusive with Native AOT (PublishAot).
    /// When built in AOT mode, DISABLE_PLUGINS is defined and Discover() returns empty.
    /// </summary>
    public static class PluginLoader
    {
        /// <summary>
        /// Dedicated load context for plugin assemblies.
        /// NOTE: isCollectible is NOT enabled because plugin assemblies are loaded once at
        /// startup and referenced for the app lifetime. If hot-reload is added in the future,
        /// enable isCollectible and store the context reference for explicit unloading.
        /// With isCollectible: false, calling Unload() would throw InvalidOperationException.
        /// AssemblyLoadContext.Dispose() is also not called — contexts live for app lifetime.
        /// </summary>
        private sealed class PluginLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _resolver;

            public PluginLoadContext(string pluginDir) : base(isCollectible: false)
            {
                _resolver = new AssemblyDependencyResolver(pluginDir);
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
            }
        }

        /// <summary>
        /// Discovers plugins in the plugins/ directory beside the executable.
        /// Returns loaded manifests. Failed loads produce PluginLoadResult entries with errors.
        /// When AOT is active, returns empty — plugin loading is not supported in AOT builds.
        /// </summary>
        public static IReadOnlyList<PluginLoadResult> Discover()
        {
#if DISABLE_PLUGINS
            return Array.Empty<PluginLoadResult>();
#else
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            if (!Directory.Exists(pluginDir))
                return Array.Empty<PluginLoadResult>();

            var results = new List<PluginLoadResult>();
            var loadedPluginIds = new HashSet<string>();

            foreach (var dll in Directory.GetFiles(pluginDir, "*.dll"))
            {
                try
                {
                    var loadContext = new PluginLoadContext(pluginDir);
                    var assembly = loadContext.LoadFromAssemblyPath(dll);
                    var manifestType = assembly.GetTypes()
                        .FirstOrDefault(t => typeof(IPluginManifest).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

                    if (manifestType == null)
                    {
                        results.Add(PluginLoadResult.Skipped(dll, "No IPluginManifest implementation found"));
                        continue;
                    }

                    var manifest = (IPluginManifest)Activator.CreateInstance(manifestType)!;

                    // Check for collision with built-in plugins (must be registered before Discover() is called)
                    if (PluginRegistry.Instance.BuiltInManifests.Any(m => m.PluginId == manifest.PluginId))
                    {
                        Native.OS.LogException(new InvalidOperationException(
                            $"Plugin '{manifest.DisplayName}' (PluginId '{manifest.PluginId}') from '{dll}' " +
                            $"collides with an already-registered built-in plugin. Skipping external version."));
                        results.Add(PluginLoadResult.Skipped(dll,
                            $"PluginId '{manifest.PluginId}' already registered as built-in"));
                        continue;
                    }

                    // Validate PluginId uniqueness across loaded plugins
                    if (loadedPluginIds.Contains(manifest.PluginId))
                    {
                        results.Add(PluginLoadResult.Skipped(dll,
                            $"Duplicate PluginId '{manifest.PluginId}' — already loaded from another DLL"));
                        continue;
                    }

                    loadedPluginIds.Add(manifest.PluginId);
                    results.Add(PluginLoadResult.Success(manifest, dll));
                }
                catch (Exception ex)
                {
                    Native.OS.LogException(new InvalidOperationException(
                        $"Failed to load plugin from '{dll}': {ex.Message}", ex));
                    results.Add(PluginLoadResult.Failed(dll, ex));
                }
            }

            return results;
#endif
        }
    }
}
