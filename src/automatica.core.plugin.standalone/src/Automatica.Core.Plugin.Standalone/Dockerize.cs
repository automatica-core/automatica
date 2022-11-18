using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Plugin.Standalone.Factories;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;

namespace Automatica.Core.Plugin.Standalone
{
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

    internal static class Dockerize
    {
        internal static void InitFactory<T, T2>(T factory, T2 templateFactory) where T : IFactory<T2> where T2 : IPropertyTemplateFactory, IRemoteFactory
        {
            factory.InitTemplates(templateFactory);
        }

        internal static void InitDriverFactory<T2>(IDriverFactory factory, T2 templateFactory) where T2 : INodeTemplateFactory
        {
            factory.InitTemplates(templateFactory);
        }

        internal static Task<IList<T>> Init<T>(string workingDir, ILogger logger, ILocalizationProvider localizationProvider)
        {
            
            IList<T> retT = new List<T>();
            foreach (var file in Directory.GetFiles(workingDir, "*Factory*.dll"))
            {

                var loader = new AssemblyLoader(workingDir, workingDir);
                Assembly AssemblyLoader(AssemblyLoadContext context, AssemblyName name)
                {
                    logger.LogInformation($"Try to load assembly {name} for {file}");
                    // avoid loading *.resources dlls, because of: https://github.com/dotnet/coreclr/issues/8416
                    if (name.Name.EndsWith("resources"))
                    {
                        return null;
                    }

                    logger.LogInformation($"try to load assembly from {workingDir}");
                    var foundDlls = Directory.GetFileSystemEntries(workingDir, name.Name + ".dll", SearchOption.AllDirectories);
                    if (foundDlls.Any())
                    {
                        return context.LoadFromAssemblyPath(foundDlls[0]);
                    }

                    var secondPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    logger.LogDebug($"try to load assembly from second path: {secondPath}");
                    foundDlls = Directory.GetFileSystemEntries(secondPath, name.Name + ".dll", SearchOption.AllDirectories);
                    if (foundDlls.Any())
                    {
                        return context.LoadFromAssemblyPath(foundDlls[0]);
                    }

                    return context.LoadFromAssemblyName(name);
                }

                Assembly assembly;
                try
                {
                    AssemblyLoadContext.Default.Resolving += AssemblyLoader;
                    logger.LogInformation($"Loading file {file}");
                    assembly = loader.LoadFromAssemblyPath(file);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not load assembly {file}");
                    continue;
                }
                finally
                {
                    AssemblyLoadContext.Default.Resolving -= AssemblyLoader;
                }

                var resources = assembly.GetManifestResourceNames().SingleOrDefault(a => a.EndsWith("automatica-manifest.json"));

                if (resources == null)
                {
                    continue;
                }

                var manifest = Common.Update.Plugin.GetEmbeddedPluginManifest(logger, assembly);

                if (manifest.Automatica.Type == "driver")
                {
                    var drivers = assembly.GetExportedTypes().Where(a => a.IsSubclassOf(typeof(T)));

                    foreach (var driver in drivers)
                    {
                        if (driver.IsAbstract)
                        {
                            continue;
                        }
                        if (assembly.CreateInstance(driver.FullName) is T factory)
                        {
                            retT.Add(factory);
                        }
                    }
                }

                localizationProvider.LoadFromAssembly(assembly);
            }

            return Task.FromResult(retT);
        }

    }
}
