using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Automatica.Core.EF.Models;
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
            var deps = DependencyContext.Default;
            var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
            var name = res.FirstOrDefault();

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
        public static IList<T> Load<T>(string folder, string searchPattern, ILogger logger, AutomaticaContext database, bool isInDevMode)
        {
            logger.LogDebug($"Loading files from {folder}");
            var folders = Directory.GetDirectories(folder);
            var rootItems = LoadFromFolder<T>(folder, searchPattern, logger,database, isInDevMode);

            var items = LoadFolders<T>(folders, searchPattern, 1, logger, database, isInDevMode);

            items.AddRange(rootItems);
            return items;
        }

        private static List<T> LoadFolders<T>(string[] folders, string searchPattern, int recCount, ILogger logger, AutomaticaContext database, bool isInDevMode)
        {
            var list = new List<T>();
            if (recCount == 10)
            {
                return new List<T>();
            }
            

            foreach (var folder in folders)
            {
                var items = LoadFromFolder<T>(folder, searchPattern, logger, database, isInDevMode);
                if (items.Count > 0)
                {
                    list.AddRange(items);
                }

                var subFolders = Directory.GetDirectories(folder);
                var loaded = LoadFolders<T>(subFolders, searchPattern, recCount++, logger, database, isInDevMode);

                if (loaded != null && loaded.Count > 0)
                {
                    list.AddRange(loaded);
                }
            }
            return list;
        }

        private static bool IsCandidateLibrary(RuntimeLibrary library, AssemblyName assemblyName)
        {
            return (library.Name == (assemblyName.Name))
                    || (library.Dependencies.Any(d => d.Name.StartsWith(assemblyName.Name)));
        }

        public static List<T> LoadSingle<T>(string file, ILogger logger, AutomaticaContext database, bool isInDevMode)
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
                    loader = new AssemblyLoader(folder, new FileInfo(Assembly.GetCallingAssembly().Location).DirectoryName, folder);
                }
                else
                {
                    loader = new AssemblyLoader(folder, folder);

                }

                Func<AssemblyLoadContext, AssemblyName, Assembly> assemblyLoader = (context, name) =>
                {
                    logger.LogDebug($"Try to load assembly {name} for {file}");
                    // avoid loading *.resources dlls, because of: https://github.com/dotnet/coreclr/issues/8416
                    if (name.Name.EndsWith("resources"))
                    {
                        return null;
                    }

                    logger.LogDebug($"try to load assemably from {folder}");
                    var foundDlls = Directory.GetFileSystemEntries(folder, name.Name + ".dll", SearchOption.AllDirectories);
                    if (foundDlls.Any())
                    {
                        return context.LoadFromAssemblyPath(foundDlls[0]);
                    }

                    var secondPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    logger.LogDebug($"try to load assemably from second path: {secondPath}");
                    foundDlls = Directory.GetFileSystemEntries(secondPath, name.Name + ".dll", SearchOption.AllDirectories);
                    if (foundDlls.Any())
                    {
                        return context.LoadFromAssemblyPath(foundDlls[0]);
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
                    logger.LogError(e, $"Could not load assembly");
                    return list;
                }
                finally
                {
                    AssemblyLoadContext.Default.Resolving -= assemblyLoader;
                }

                var resources = assembly.GetManifestResourceNames().SingleOrDefault(a => a.EndsWith("automatica-manifest.json"));

                if (resources == null)
                {
                    logger.LogWarning($"{file} does not contain a manifest files");
                    throw new NoManifestFoundException();
                }

                var manifest = Common.Update.Plugin.GetEmbeddedPluginManifest(logger, assembly);
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
                database.SaveChanges();

                foreach (var ti in assembly.ExportedTypes)
                {
                    if (ti.IsSubclassOf(typeof(T)))
                    {
                        if (ti.IsAbstract)
                        {
                            continue;
                        }
                        if (assembly.CreateInstance(ti.FullName) is T factory)
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
            catch (Exception e)
            {
                logger.LogError(e, $"Could not load {typeof(T)}...{e}");
            }
            return list;
        }

        private static List<T> LoadFromFolder<T>(string folder, string searchPattern, ILogger logger, AutomaticaContext database, bool isInDevMode)
        {
            logger.LogDebug($"Loading files from {folder} and check for {typeof(T)}");
            var list = new List<T>();
            foreach (var file in Directory.GetFiles(folder, searchPattern))
            {
                try
                {
                    list.AddRange(LoadSingle<T>(file, logger, database, isInDevMode));
                }
                catch (NoManifestFoundException)
                {
                    continue;
                }
            }

            return list;
        }
    }
}
