using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Base.BoardType;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Runtime.Abstraction.Plugins;
using Automatica.Core.Runtime.Abstraction.Plugins.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Automatica.Core.Tests")]

namespace Automatica.Core.Runtime.Core.Plugins.Drivers
{
    internal class DriverLoader : IDriverLoader
    {
        private readonly ILogger<DriverLoader> _logger;
        private readonly AutomaticaContext _dbContext;
        private readonly ILocalizationProvider _localizationProvider;
        private readonly IConfiguration _config;
        private readonly ILoadedStore _store;
        private readonly IDriverFactoryStore _driverFactoryStore;
        private readonly INodeInstanceService _nodeInstanceService;
        private readonly IServiceProvider _serviceProvider;

        public DriverLoader(
            ILogger<DriverLoader> logger, AutomaticaContext dbContext, 
            ILocalizationProvider localizationProvider, IConfiguration config, 
            ILoadedStore store, IDriverFactoryStore driverFactoryStore,
            INodeInstanceService nodeInstanceService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _dbContext = dbContext;
            _localizationProvider = localizationProvider;
            _config = config;
            _store = store;
            _driverFactoryStore = driverFactoryStore;
            _nodeInstanceService = nodeInstanceService;
            _serviceProvider = serviceProvider;
        }

        public async Task Load(IDriverFactory factory, IBoardType boardType)
        {
            try
            {
                var manifest = Common.Update.Plugin.GetEmbeddedPluginManifest(_logger, factory.GetType().Assembly);

                if (manifest == null)
                {
                    throw new NoManifestFoundException();
                }

                _store.Add(manifest.Automatica.PluginGuid, manifest);
                _driverFactoryStore.AddDriver(factory, manifest);
                
                _logger.LogDebug($"Init driver {factory.DriverName} {factory.DriverVersion}...");

                var driverDbVersion =
                    _dbContext.VersionInformations.SingleOrDefault(a => a.DriverGuid == factory.DriverGuid);
                var initNodeTemplates = false;

                if (driverDbVersion == null)
                {
                    driverDbVersion = new VersionInformation
                    {
                        Name = factory.DriverName,
                        Version = factory.DriverVersion.ToString(),
                        DriverGuid = factory.DriverGuid
                    };
                    initNodeTemplates = true;
                    _dbContext.VersionInformations.Add(driverDbVersion);
                }
                else if (factory.DriverVersion > driverDbVersion.VersionData)
                {
                    initNodeTemplates = true;
                    driverDbVersion.Name = factory.DriverName;
                    driverDbVersion.Version = factory.DriverVersion.ToString();
                }

                if (!UsesInterface(boardType, factory))
                {
                    _logger.LogInformation(
                        $"Ignore {factory.DriverName} because we do not support any of the given interfaces");
                    return;
                }

                _localizationProvider.LoadFromAssembly(factory.GetType().Assembly);
                if (initNodeTemplates || factory.InDevelopmentMode ||
                    factory.GetType().Assembly.GetName().Version < new Version(2, 3))
                {
                    var logicTemplateFactoryProvider = new NodeTemplateFactoryProvider(_serviceProvider);
                    var nodeTemplateFactory = logicTemplateFactoryProvider.CreateInstance(factory.DriverGuid);
                    
                    nodeTemplateFactory.SetFactory(factory);
                    _logger.LogDebug($"InitNodeTemplates for {factory.DriverName}...");
                    factory.InitNodeTemplates(nodeTemplateFactory);
                    await nodeTemplateFactory.CommitChanges();


                    _logger.LogDebug($"InitNodeTemplates for {factory.DriverName}...done");
                }
                else
                {
                    await using var db = new AutomaticaContext(_config);
                    factory.InitNodeTemplates(new DoNothingNodeTemplateFactory(db, _config));
                }


                _logger.LogDebug($"Init driver {factory.DriverName} {factory.DriverVersion}...done");

                await _dbContext.SaveChangesAsync();

            }
            catch (NoManifestFoundException)
            {
                // ignore
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load driver {factory.DriverName} {e}", e);
            }

        }

        private bool UsesInterface(IBoardType boardType, IDriverFactory factory)
        {
            foreach (var t in boardType.ProvidesInterfaceTypes)
            {
                if (factory.UsesInterfaces.Contains(t))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
