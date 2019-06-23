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
    internal static class Dockerize
    {
        internal static async Task InitFactory<T, T2>(IMqttClient client, T factory, T2 templateFactory) where T : IFactory<T2> where T2 : IPropertyTemplateFactory, IRemoteFactory
        {
            factory.InitTemplates(templateFactory);

            await templateFactory.SubmitFactoryData(factory.FactoryGuid, client);
        }

        internal static async Task InitDriverFactory<T2>(IMqttClient client, DriverFactory factory, T2 templateFactory) where T2 : INodeTemplateFactory, IRemoteFactory
        {
            factory.InitTemplates(templateFactory);

            await templateFactory.SubmitFactoryData(factory.FactoryGuid, client);
        }

        internal static Task<IList<T>> Init<T>(string workingDir, ILogger logger, ILocalizationProvider localizationProvider)
        {

            IList<T> retT = new List<T>();
            foreach (var file in Directory.GetFiles(workingDir, "*.dll"))
            {
                Assembly AssemblyLoader(AssemblyLoadContext context, AssemblyName name)
                {
                    logger.LogDebug($"Try to load assembly {name} for {file}");
                    // avoid loading *.resources dlls, because of: https://github.com/dotnet/coreclr/issues/8416
                    if (name.Name.EndsWith("resources"))
                    {
                        return null;
                    }

                    logger.LogDebug($"try to load assembly from {workingDir}");
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
                    assembly = Assembly.LoadFrom(file);
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
