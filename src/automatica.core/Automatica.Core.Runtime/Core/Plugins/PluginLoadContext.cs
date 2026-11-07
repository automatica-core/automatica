using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Automatica.Core.Base.Common;

namespace Automatica.Core.Runtime.Core.Plugins
{
    /// <summary>
    /// One collectible load context per plugin directory, so a plugin can be unloaded and a new version loaded without a restart.
    /// Everything the core ships itself (Automatica.Core.*, Microsoft.*, ...) comes from the default context: the plugin and the core
    /// must share these types. Only the assemblies the core does not have are loaded from the plugin directory.
    /// A library the plugin does not ship itself is taken from another plugin directory (as its own copy), like before the
    /// load contexts all plugins shared their libraries (e.g. shelly uses PureWebSockets, only loxone ships it).
    /// </summary>
    internal sealed class PluginLoadContext : AssemblyLoadContext
    {
        private static readonly Lazy<HashSet<string>> HostAssemblies = new(LoadHostAssemblies);

        private readonly AssemblyDependencyResolver? _resolver;
        private readonly string? _sharedRoot;

        public string Folder { get; }

        /// <summary>
        /// Native libraries can not be unloaded, a plugin that loaded one is updated with a restart
        /// </summary>
        public bool NativeLibrariesLoaded { get; internal set; }

        /// <param name="folder">the plugin directory</param>
        /// <param name="sharedRoot">the directory with all plugin directories, searched for libraries the plugin does not ship</param>
        public PluginLoadContext(string folder, string? sharedRoot = null) : base($"plugin:{Path.GetFileName(folder)}", isCollectible: true)
        {
            Folder = folder;
            _sharedRoot = sharedRoot;
            _resolver = CreateResolver(folder);
        }

        private static AssemblyDependencyResolver? CreateResolver(string folder)
        {
            // the plugin's deps.json knows its private and runtime specific (native) dependencies
            var depsFile = Directory.GetFiles(folder, "*.deps.json").FirstOrDefault();
            if (depsFile == null)
            {
                return null;
            }

            var component = Path.Combine(folder, Path.GetFileName(depsFile)[..^".deps.json".Length] + ".dll");
            if (!File.Exists(component))
            {
                return null;
            }

            try
            {
                return new AssemblyDependencyResolver(component);
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == null || IsHostAssembly(assemblyName))
            {
                return null;
            }

            var path = _resolver?.ResolveAssemblyToPath(assemblyName);
            if (path == null)
            {
                var candidate = Path.Combine(Folder, assemblyName.Name + ".dll");
                if (File.Exists(candidate))
                {
                    path = candidate;
                }
            }

            path ??= FindInOtherPlugins(assemblyName);

            return path == null ? null : LoadFromAssemblyPath(path);
        }

        /// <summary>
        /// The highest version of the library in another plugin directory, at least the requested version
        /// </summary>
        internal string? FindInOtherPlugins(AssemblyName assemblyName)
        {
            if (_sharedRoot == null || !Directory.Exists(_sharedRoot))
            {
                return null;
            }

            var options = new EnumerationOptions { RecurseSubdirectories = true, MatchCasing = MatchCasing.CaseInsensitive, IgnoreInaccessible = true };
            string? best = null;
            Version? bestVersion = null;
            foreach (var file in Directory.EnumerateFiles(_sharedRoot, assemblyName.Name + ".dll", options))
            {
                Version? version;
                try
                {
                    version = AssemblyName.GetAssemblyName(file).Version;
                }
                catch (Exception)
                {
                    continue; // native or broken file
                }

                if (assemblyName.Version != null && (version == null || version < assemblyName.Version))
                {
                    continue;
                }

                if (best == null || (version != null && (bestVersion == null || version > bestVersion)))
                {
                    best = file;
                    bestVersion = version;
                }
            }

            return best;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver?.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (path == null)
            {
                return IntPtr.Zero;
            }

            NativeLibrariesLoaded = true;
            return LoadUnmanagedDllFromPath(path);
        }

        /// <summary>
        /// Loads an assembly of the plugin directory, an assembly with the same name that is already loaded is returned instead
        /// </summary>
        public Assembly LoadPluginAssembly(string file)
        {
            var name = AssemblyName.GetAssemblyName(file);
            var loaded = Assemblies.FirstOrDefault(a => string.Equals(a.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));
            return loaded ?? LoadFromAssemblyPath(file);
        }

        public bool Owns(object? instance)
        {
            return instance != null && GetLoadContext(instance.GetType().Assembly) == this;
        }

        internal static bool IsHostAssembly(AssemblyName assemblyName)
        {
            return assemblyName.Name != null && HostAssemblies.Value.Contains(assemblyName.Name);
        }

        private static HashSet<string> LoadHostAssemblies()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // the framework and every dependency in the core's deps.json
            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa)
            {
                foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    set.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            foreach (var path in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
            {
                set.Add(Path.GetFileNameWithoutExtension(path));
            }

            return set;
        }
    }

    /// <summary>
    /// The load contexts of the loaded plugins, by plugin directory
    /// </summary>
    internal static class PluginLoadContexts
    {
        private static readonly ConcurrentDictionary<string, PluginLoadContext> Contexts =
            new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        public static string Normalize(string folder)
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        }

        public static PluginLoadContext GetOrCreate(string folder)
        {
            return Contexts.GetOrAdd(Normalize(folder), f => new PluginLoadContext(f, SharedRoot(f)));
        }

        /// <summary>
        /// The plugin directory (with Drivers and Rules) when the folder is inside it
        /// </summary>
        private static string? SharedRoot(string folder)
        {
            if (string.IsNullOrEmpty(ServerInfo.PluginDirectory))
            {
                return null;
            }

            var root = Normalize(ServerInfo.PluginDirectory);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return folder.StartsWith(root + Path.DirectorySeparatorChar, comparison) ? root : null;
        }

        public static bool TryGet(string folder, out PluginLoadContext context)
        {
            return Contexts.TryGetValue(Normalize(folder), out context!);
        }

        public static bool Contains(string folder)
        {
            return Contexts.ContainsKey(Normalize(folder));
        }

        /// <summary>
        /// Starts unloading the plugin directory's context. It is gone once nothing references its types any more (see the returned reference).
        /// </summary>
        public static WeakReference? Unload(string folder)
        {
            if (!Contexts.TryRemove(Normalize(folder), out var context))
            {
                return null;
            }

            var weak = new WeakReference(context);
            context.Unload();
            return weak;
        }
    }
}
