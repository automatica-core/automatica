using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
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
        // names, not the assemblies: the runtime holds an unloading context strongly, assemblies held by it would keep it loaded forever
        private readonly HashSet<string> _resolved = new(StringComparer.Ordinal);

        /// <summary>
        /// The plugin directory (the key of the context)
        /// </summary>
        public string Folder { get; }

        /// <summary>
        /// The directory the assemblies are loaded from: the plugin directory, or a copy of it when the plugin is loaded again in the same process (see <see cref="PluginLoadContexts"/>)
        /// </summary>
        public string LoadFolder { get; }

        /// <summary>
        /// Native libraries can not be unloaded, a plugin that loaded one is updated with a restart
        /// </summary>
        public bool NativeLibrariesLoaded { get; internal set; }

        /// <param name="folder">the plugin directory</param>
        /// <param name="sharedRoot">the directory with all plugin directories, searched for libraries the plugin does not ship</param>
        /// <param name="loadFolder">a copy of the plugin directory to load the assemblies from, null to load them from <paramref name="folder"/></param>
        public PluginLoadContext(string folder, string? sharedRoot = null, string? loadFolder = null) : base($"plugin:{Path.GetFileName(folder)}", isCollectible: true)
        {
            Folder = folder;
            LoadFolder = loadFolder ?? folder;
            _sharedRoot = sharedRoot;
            _resolver = CreateResolver(LoadFolder);
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

        /// <summary>
        /// Raised for every call of <see cref="Load"/>, i.e. every time the runtime runs managed code to find an assembly (tests)
        /// </summary>
        internal event Action<AssemblyName>? LoadRequested;

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            LoadRequested?.Invoke(assemblyName);

            if (assemblyName.Name == null || IsHostAssembly(assemblyName))
            {
                return null;
            }

            var path = _resolver?.ResolveAssemblyToPath(assemblyName);
            if (path == null)
            {
                var candidate = Path.Combine(LoadFolder, assemblyName.Name + ".dll");
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
        /// Loads an assembly of the plugin directory (from <see cref="LoadFolder"/>), an assembly with the same name that is already loaded is returned instead
        /// </summary>
        public Assembly LoadPluginAssembly(string file)
        {
            var name = AssemblyName.GetAssemblyName(file);
            var loaded = Assemblies.FirstOrDefault(a => string.Equals(a.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));
            var assembly = loaded ?? LoadFromAssemblyPath(ToLoadPath(file));
            ResolveReferences(assembly);
            return assembly;
        }

        /// <summary>
        /// Resolves the references of the assembly, and of every assembly of this context it uses, now instead of on first use.
        /// The runtime asks <see cref="Load"/> (managed code) the first time a module needs one of its references. That can happen
        /// inside a garbage collection: it reads the argument types of a method that is being compiled, e.g. the
        /// WebSocketCloseStatus of an OnClosed handler. The managed code allocates, waits for the end of the GC and the whole process
        /// hangs (automatica10_2, 2026-11-11). A reference the module resolved once is kept in the module, so this happens only here.
        /// </summary>
        internal void ResolveReferences(Assembly assembly)
        {
            var pending = new Queue<Assembly>();
            if (MarkResolved(assembly))
            {
                pending.Enqueue(assembly);
            }

            while (pending.TryDequeue(out var next))
            {
                foreach (var referenced in ResolveModuleReferences(next))
                {
                    // the core's and the framework's assemblies are in the default context, it finds their references without managed code
                    if (GetLoadContext(referenced) == this && MarkResolved(referenced))
                    {
                        pending.Enqueue(referenced);
                    }
                }
            }
        }

        private bool MarkResolved(Assembly assembly)
        {
            lock (_resolved)
            {
                return _resolved.Add(assembly.FullName ?? assembly.Location);
            }
        }

        /// <summary>
        /// Resolves one type reference per assembly reference through the module, the same way the runtime does on first use
        /// </summary>
        /// <returns>the referenced assemblies that could be loaded</returns>
        private static List<Assembly> ResolveModuleReferences(Assembly assembly)
        {
            var referenced = new List<Assembly>();
            if (string.IsNullOrEmpty(assembly.Location) || !File.Exists(assembly.Location))
            {
                return referenced;
            }

            try
            {
                using var stream = File.OpenRead(assembly.Location);
                using var pe = new PEReader(stream);
                if (!pe.HasMetadata)
                {
                    return referenced;
                }

                var metadata = pe.GetMetadataReader();
                var module = assembly.ManifestModule;
                var done = new HashSet<AssemblyReferenceHandle>();

                foreach (var handle in metadata.TypeReferences)
                {
                    var scope = metadata.GetTypeReference(handle).ResolutionScope;
                    if (scope.Kind != HandleKind.AssemblyReference)
                    {
                        // nested or module local type, its outer type resolves the assembly
                        continue;
                    }

                    var reference = (AssemblyReferenceHandle)scope;
                    if (done.Contains(reference))
                    {
                        continue;
                    }

                    try
                    {
                        referenced.Add(module.ResolveType(MetadataTokens.GetToken(handle)).Assembly);
                        done.Add(reference);
                    }
                    catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException)
                    {
                        // the assembly is not there: an optional dependency, it fails the same way on first use
                        done.Add(reference);
                    }
                    catch (Exception)
                    {
                        // this type is not in the assembly (other version), another type of the same reference resolves it
                    }
                }
            }
            catch (Exception)
            {
                // unreadable file: the references are resolved on first use, as before
            }

            return referenced;
        }

        /// <summary>
        /// The path of a file of the plugin directory in <see cref="LoadFolder"/>
        /// </summary>
        internal string ToLoadPath(string file)
        {
            if (LoadFolder == Folder)
            {
                return file;
            }

            var relative = Path.GetRelativePath(Path.GetFullPath(Folder), Path.GetFullPath(file));
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                // not a file of the plugin directory
                return file;
            }

            var copy = Path.Combine(LoadFolder, relative);
            return File.Exists(copy) ? copy : file;
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
    /// The load contexts of the loaded plugins, by plugin directory.
    /// The first load of a plugin directory loads its files in place. A directory that was loaded before in this process (a plugin update
    /// without a restart) is loaded from a fresh copy: as long as the old context is not collected (a static event, a thread that did not
    /// stop,...) the runtime returns the old image for a path it already loaded, even if the file was replaced (seen on linux), and the old
    /// version would keep running. The copies of earlier processes are deleted when the first copy is made.
    /// </summary>
    internal static class PluginLoadContexts
    {
        internal const string CopyDirectoryName = "plugin-load";

        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        private static readonly ConcurrentDictionary<string, PluginLoadContext> Contexts = new(PathComparer);
        private static readonly ConcurrentDictionary<string, byte> LoadedBefore = new(PathComparer);
        private static readonly object CreateLock = new();
        private static readonly Lazy<string> ProcessRunName = new(() =>
            $"{System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks}-{Environment.ProcessId}");

        private static string? _processCopyRoot;

        /// <summary>
        /// The directory for the copies of all processes, null for the staging directory (tests use their own)
        /// </summary>
        internal static string? CopyRoot
        {
            get => _copyRoot;
            set
            {
                _copyRoot = value;
                _processCopyRoot = null;
            }
        }

        private static string? _copyRoot;

        public static string Normalize(string folder)
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        }

        public static PluginLoadContext GetOrCreate(string folder)
        {
            var key = Normalize(folder);
            if (Contexts.TryGetValue(key, out var existing))
            {
                return existing;
            }

            lock (CreateLock)
            {
                if (Contexts.TryGetValue(key, out existing))
                {
                    return existing;
                }

                var loadFolder = LoadedBefore.ContainsKey(key) ? CopyForReload(key) : null;
                var context = new PluginLoadContext(key, SharedRoot(key), loadFolder);
                Contexts[key] = context;
                LoadedBefore[key] = 0;
                return context;
            }
        }

        /// <returns>a fresh copy of the plugin directory, null (load in place) if it could not be copied</returns>
        private static string? CopyForReload(string folder)
        {
            string? copy = null;
            try
            {
                copy = Path.Combine(GetProcessCopyRoot(), $"{Path.GetFileName(folder)}-{Guid.NewGuid():N}");
                CopyDirectory(folder, copy);
                return copy;
            }
            catch (Exception)
            {
                TryDelete(copy);
                return null;
            }
        }

        private static string GetProcessCopyRoot()
        {
            if (_processCopyRoot != null && Directory.Exists(_processCopyRoot))
            {
                return _processCopyRoot;
            }

            var root = CopyRoot ?? ServerInfo.EnsurePrivateDirectory(Path.Combine(ServerInfo.GetStagingDirectory(), CopyDirectoryName));
            Directory.CreateDirectory(root);
            DeleteCopiesOfEarlierProcesses(root, ProcessRunName.Value);

            _processCopyRoot = Path.Combine(root, ProcessRunName.Value);
            Directory.CreateDirectory(_processCopyRoot);
            return _processCopyRoot;
        }

        /// <summary>
        /// Deletes the copies of processes that started before this one ("&lt;start ticks&gt;-&lt;pid&gt;")
        /// </summary>
        internal static void DeleteCopiesOfEarlierProcesses(string root, string processRunName)
        {
            if (!TryParseStart(processRunName, out var ownStart))
            {
                return;
            }

            foreach (var directory in Directory.GetDirectories(root))
            {
                var name = Path.GetFileName(directory);
                if (name != processRunName && TryParseStart(name, out var start) && start < ownStart)
                {
                    TryDelete(directory);
                }
            }
        }

        private static bool TryParseStart(string name, out long start)
        {
            var dash = name.IndexOf('-');
            start = 0;
            return dash > 0 && long.TryParse(name[..dash], out start);
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
            }
        }

        private static void TryDelete(string? directory)
        {
            try
            {
                if (directory != null && Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception)
            {
                // still in use (windows), deleted by the next process
            }
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
