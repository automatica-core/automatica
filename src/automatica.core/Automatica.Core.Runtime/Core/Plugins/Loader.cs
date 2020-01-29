using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core.Plugins
{
    public class NoManifestFoundException : Exception {

    }

    public class AssemblyLoader : AssemblyLoadContext
    {
        private readonly string _currentAssemblyPath;
        private readonly string[] _path;

        public AssemblyLoader(string currentAssemblyPath, params string[] path)
        {
            _currentAssemblyPath = currentAssemblyPath;
            _path = path;
        }
        public Assembly LoadAssembly(AssemblyName assemblyName)
        {
            return Load(assemblyName);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            foreach (var path in _path)
            {
                var pathFileName = Path.Combine(path, $"{assemblyName.Name}.dll");

                if (File.Exists(pathFileName))
                {
                    if (path == _currentAssemblyPath)
                    {
                        return LoadFromAssemblyPath(pathFileName);
                    }
                    return Default.LoadFromAssemblyPath(pathFileName);
                }
            }

            string corePath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var fileName = Path.Combine(corePath, $"{assemblyName.Name}.dll");

            if (File.Exists(fileName))
            {
                return Default.LoadFromAssemblyPath(fileName);
            }

            return Default.LoadFromAssemblyName(assemblyName);
        }
    }
    public static class Loader
    {
        public static async Task<IList<T>> Load<T>(string folder, string searchPattern, ILogger logger, IConfiguration config, bool isInDevMode)
        {
            logger.LogDebug($"Loading files from {folder}");
            var folders = Directory.GetDirectories(folder);
            var rootItems = await LoadFromFolder<T>(folder, searchPattern, logger, config, isInDevMode);

            var items = await LoadFolders<T>(folders, searchPattern, 1, logger, config, isInDevMode);

            items.AddRange(rootItems);
            return items;
        }

        private static Task<List<T>> LoadFolders<T>(string[] folders, string searchPattern, int recCount,
            ILogger logger, IConfiguration config, bool isInDevMode)
        {
            var list = new List<T>();
            if (recCount == 10)
            {
                return Task.FromResult(new List<T>());
            }


            Parallel.ForEach(folders, async folder =>
            {
                var items = await LoadFromFolder<T>(folder, searchPattern, logger, config, isInDevMode);
                if (items.Count > 0)
                {
                    list.AddRange(items);
                }

                var subFolders = Directory.GetDirectories(folder);
                var loaded = await LoadFolders<T>(subFolders, searchPattern, recCount++, logger, config,
                    isInDevMode);

                if (loaded != null && loaded.Count > 0)
                {
                    list.AddRange(loaded);
                }
            });



            return Task.FromResult(list);
        }

        private static bool IsCandidateLibrary(RuntimeLibrary library, AssemblyName assemblyName)
        {
            return (library.Name == (assemblyName.Name))
                    || (library.Dependencies.Any(d => d.Name.StartsWith(assemblyName.Name)));
        }

        public static async Task<List<T>> LoadSingle<T>(string file, ILogger logger, IConfiguration config, bool isInDevMode)
        {
            var list = new List<T>();

            try
            {
                var fileInfo = new FileInfo(file);
                var folder = fileInfo.DirectoryName;
                logger.LogDebug($"Loading file {file} and check for interfaces");

                AssemblyLoader loader;
                if (isInDevMode)
                {
                    loader = new AssemblyLoader(folder,
                        new FileInfo(Assembly.GetCallingAssembly().Location).DirectoryName, folder);
                }
                else
                {
                    loader = new AssemblyLoader(folder, folder);

                }

                Func<AssemblyLoadContext, AssemblyName, Assembly> assemblyLoader = (context, name) =>
                {
                    logger.LogDebug($"Try to load assembly {name} for {file}");
                    // avoid loading *.resources dll, because of: https://github.com/dotnet/coreclr/issues/8416
                    if (name.Name.EndsWith("resources"))
                    {
                        return null;
                    }

                    logger.LogDebug($"try to load assembly from {folder}");
                    var foundDll =
                        Directory.GetFileSystemEntries(folder, name.Name + ".dll", SearchOption.AllDirectories);
                    if (foundDll.Any())
                    {
                        return context.LoadFromAssemblyPath(foundDll[0]);
                    }

                    var secondPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    logger.LogDebug($"try to load assembly from second path: {secondPath}");
                    foundDll = Directory.GetFileSystemEntries(secondPath, name.Name + ".dll",
                        SearchOption.AllDirectories);
                    if (foundDll.Any())
                    {
                        return context.LoadFromAssemblyPath(foundDll[0]);
                    }

                    var dependencies = DependencyContext.Default.RuntimeLibraries;
                    foreach (var library in dependencies)
                    {
                        if (IsCandidateLibrary(library, name))
                        {
                            return loader.LoadAssembly(name);
                        }
                    }


                    return context.LoadFromAssemblyName(name);
                };
                AssemblyLoadContext.Default.Resolving += assemblyLoader;

                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(file);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not load assembly {file}");
                    return list;
                }
                finally
                {
                    AssemblyLoadContext.Default.Resolving -= assemblyLoader;
                }

                var resources = assembly.GetManifestResourceNames()
                    .SingleOrDefault(a => a.EndsWith("automatica-manifest.json"));

                if (resources == null)
                {
                    logger.LogWarning($"{file} does not contain a manifest files");
                    throw new NoManifestFoundException();
                }

                var manifest = Common.Update.Plugin.GetEmbeddedPluginManifest(logger, assembly);
                await using var database = new AutomaticaContext(config);

                var plugin = database.Plugins.SingleOrDefault(a => a.PluginGuid == manifest.Automatica.PluginGuid);
                if (plugin == null)
                {
                    plugin = new Plugin
                    {
                        ObjId = Guid.NewGuid(),
                        Name = manifest.Automatica.Name,
                        ComponentName = manifest.Automatica.ComponentName,
                        PluginGuid = manifest.Automatica.PluginGuid,
                        PluginType = manifest.Automatica.Type == "driver" ? PluginType.Driver : PluginType.Logic,
                        Version = manifest.Automatica.PluginVersion.ToString(),
                        Loaded = true
                    };
                    database.Plugins.Add(plugin);
                }
                else
                {
                    plugin.Version = manifest.Automatica.PluginVersion.ToString();
                    plugin.Loaded = true;
                    database.Plugins.Update(plugin);
                }

                await database.SaveChangesAsync();

                foreach (var ti in assembly.ExportedTypes)
                {
                    if (ti.IsSubclassOf(typeof(T)))
                    {
                        if (ti.IsAbstract)
                        {
                            continue;
                        }

                        if (assembly.CreateInstance(ti.FullName ?? throw new InvalidOperationException()) is T factory)
                        {
                            logger.LogDebug($"Found {typeof(T)} in {ti}...");
                            list.Add(factory);
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException e)
            {
                var builder = new StringBuilder();
                foreach (var ex in e.LoaderExceptions)
                {
                    builder.Append($"{ex}");
                }

                logger.LogError(e.InnerException, $"Could not load {typeof(T)} from {file}...{builder}\n");
            }
            catch (NoManifestFoundException)
            {
                // ignore
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not load {typeof(T)}...{e}");
            }
            return list;
        }

        private static async Task<List<T>> LoadFromFolder<T>(string folder, string searchPattern, ILogger logger, IConfiguration config, bool isInDevMode)
        {
            logger.LogDebug($"Loading files from {folder} and check for {typeof(T)}");
            var list = new List<T>();
            foreach (var file in Directory.GetFiles(folder, searchPattern))
            {
                try
                {
                    list.AddRange(await LoadSingle<T>(file, logger, config, isInDevMode));
                }
                catch (NoManifestFoundException)
                {
                    
                }
            }

            return list;
        }
    }
}
