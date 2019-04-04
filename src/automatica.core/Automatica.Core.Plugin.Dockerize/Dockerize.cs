using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Plugin.Dockerize.Factories;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Automatica.Core.Plugin.Dockerize
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

        internal static Task<IList<T>> Init<T>(string workingDir, ILogger logger)
        {
            IList<T> retT = new List<T>();
            foreach (var file in Directory.GetFiles(workingDir, "*.dll"))
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(file);
                }
                catch (Exception)
                {
                    continue;
                }
                finally
                {
                    //AssemblyLoadContext.Default.Resolving -= assemblyLoader;
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
            }

            return Task.FromResult(retT);
        }
    }
}
