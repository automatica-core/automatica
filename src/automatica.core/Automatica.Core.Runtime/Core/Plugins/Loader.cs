using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Common.Update.Signing;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core.Plugins
{
    public class NoManifestFoundException : Exception {

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

        private static async Task<List<T>> LoadFolders<T>(string[] folders, string searchPattern, int recCount,
            ILogger logger, IConfiguration config, bool isInDevMode)
        {
            var list = new List<T>();
            if (recCount == 10)
            {
                return new List<T>();
            }

            foreach(var folder in folders)
            {
                var items = await LoadFromFolder<T>(folder, searchPattern, logger, config, isInDevMode);
                if (items.Count > 0)
                {
                    list.AddRange(items);
                }

                var subFolders = Directory.GetDirectories(folder);
                var loaded = await LoadFolders<T>(subFolders, searchPattern, recCount++, logger, config,
                    isInDevMode);

                if (loaded.Count > 0)
                {
                    list.AddRange(loaded);
                }
            }

            return list;
        }

        public static async Task<List<T>> LoadSingle<T>(string file, ILogger logger, IConfiguration config, bool isInDevMode)
        {
            var list = new List<T>();

            try
            {
                var fileInfo = new FileInfo(file);
                var folder = fileInfo.DirectoryName;
                logger.LogInformation($"Loading file {file} and check for interfaces");

                // one collectible context per plugin directory: the plugin can be unloaded and updated without a restart
                var context = PluginLoadContexts.GetOrCreate(folder!);

                Assembly assembly;
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(file);
                    if (PluginLoadContext.IsHostAssembly(assemblyName))
                    {
                        if (!isInDevMode)
                        {
                            // a copy of a core assembly (e.g. Automatica.Core.Driver.dll) is never the plugin, the core's own is used
                            logger.LogDebug($"{file} is shipped by the core, it is not loaded from the plugin directory");
                            return list;
                        }

                        // development: drivers referenced by the core project are loaded with the core (no update without restart)
                        assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
                    }
                    else
                    {
                        assembly = context.LoadPluginAssembly(file);
                    }
                }
                catch (BadImageFormatException)
                {
                    // native library
                    return list;
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not load assembly {file}");
                    return list;
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
                            logger.LogInformation($"Found {typeof(T)} in {ti}...");
                            list.Add(factory);
                        }
                    }
                }

                foreach (var ti in assembly.DefinedTypes)
                {
                    if (ti.IsSubclassOf(typeof(T)))
                    {
                        if (ti.IsNotPublic)
                        {
                            logger.LogWarning($"Found type {typeof(T)} in {ti} but it is not public! Ignore it!");
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
            var files = Directory.GetFiles(folder, searchPattern);

            if (files.Length > 0 && !isInDevMode && !IsFolderTrusted(folder, logger))
            {
                return list;
            }

            var contextExisted = PluginLoadContexts.Contains(folder);
            foreach (var file in files)
            {
                try
                {
                    list.AddRange(await LoadSingle<T>(file, logger, config, isInDevMode));
                }
                catch (NoManifestFoundException)
                {
                    
                }
            }

            if (list.Count == 0 && !contextExisted)
            {
                // no plugin in this directory (e.g. a sub directory with dependencies), do not keep its assemblies loaded
                PluginLoadContexts.Unload(folder);
            }

            return list;
        }

        /// <summary>
        /// An installed plugin directory with a signature file must match it exactly (no unsigned or modified file).
        /// Plugins installed before package signing have no signature file: they are loaded with a warning, unless
        /// <see cref="PackageSecurityPolicy.RequireSignedPluginsEnv"/> is set.
        /// </summary>
        internal static bool IsFolderTrusted(string folder, ILogger logger)
        {
            try
            {
                var result = PackageVerifier.VerifyDirectory(folder, PackageSignatureFormat.TypePlugin, null, null, false, true);

                switch (result.Status)
                {
                    case PackageVerificationStatus.Valid:
                        return true;
                    case PackageVerificationStatus.Unsigned when !PackageSecurityPolicy.RequireSignedPlugins:
                        logger.LogWarning($"Plugin directory {folder} has no signature file (installed before package signing), loading it anyway");
                        return true;
                    case PackageVerificationStatus.Unsigned:
                        logger.LogError($"Plugin directory {folder} has no signature file, it is not loaded ({PackageSecurityPolicy.RequireSignedPluginsEnv} is set)");
                        return false;
                    default:
                        logger.LogError($"Plugin directory {folder} does not match its signature, it is not loaded: {result.Error}");
                        return false;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not verify the plugin directory {folder}, it is not loaded");
                return false;
            }
        }
    }
}
