using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.BoardType;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.Abstraction.Plugins;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core.Plugins.Logics
{
    internal class LogicLoader : ILogicLoader
    {
        private readonly ILogger<LogicLoader> _logger;
        private readonly AutomaticaContext _dbContext;
        private readonly ILocalizationProvider _localizationProvider;
        private readonly IConfiguration _config;
        private readonly ILoadedStore _store;
        private readonly ILogicFactoryStore _logicFactoryStore;
        private readonly ILogicTemplateCache _logicTemplateCache;
        private readonly LogicTemplateFactory _logicTemplateFactory;

        public LogicLoader(ILogger<LogicLoader> logger, 
            AutomaticaContext dbContext, 
            ILocalizationProvider localizationProvider, 
            IConfiguration config, 
            ILoadedStore store, 
            ILogicFactoryStore logicFactoryStore, 
            ILogicTemplateCache logicTemplateCache,
            LogicTemplateFactory logicTemplateFactory)
        {
            _logger = logger;
            _dbContext = dbContext;
            _localizationProvider = localizationProvider;
            _config = config;
            _store = store;
            _logicFactoryStore = logicFactoryStore;
            _logicTemplateCache = logicTemplateCache;
            _logicTemplateFactory = logicTemplateFactory;
        }

        public async Task Load(ILogicFactory factory, IBoardType boardType)
        {
            try
            {
                var manifest = Common.Update.Plugin.GetEmbeddedPluginManifest(_logger, factory.GetType().Assembly);

                if (manifest == null)
                {
                    throw new NoManifestFoundException();
                }


                _store.Add(manifest.Automatica.PluginGuid, manifest);

                _logicFactoryStore.Add(factory.LogicGuid, factory);
                _logger.LogDebug($"Init logic {factory.LogicName} {factory.LogicVersion}...");

                var driverDbVersion =
                    _dbContext.VersionInformations.SingleOrDefault(a => a.RuleGuid == factory.LogicGuid);
                var initNodeTemplates = false;

                if (driverDbVersion == null)
                {
                    driverDbVersion = new VersionInformation
                    {
                        Name = factory.LogicName,
                        Version = factory.LogicVersion.ToString(),
                        RuleGuid = factory.LogicGuid
                    };
                    initNodeTemplates = true;
                    _dbContext.VersionInformations.Add(driverDbVersion);
                }
                else if (factory.LogicVersion > driverDbVersion.VersionData)
                {
                    initNodeTemplates = true;
                    driverDbVersion.Name = factory.LogicName;
                    driverDbVersion.Version = factory.LogicVersion.ToString();
                }

                _localizationProvider.LoadFromAssembly(factory.GetType().Assembly);
                if (initNodeTemplates || factory.InDevelopmentMode)
                {
                    _logger.LogDebug($"Init logic templates for {factory.LogicName}...");

                    _logicTemplateFactory.SetFactory(factory);

                    factory.InitTemplates(_logicTemplateFactory);

                    foreach (var template in _logicTemplateFactory.LogicTemplates.Values)
                    {
                        _logicTemplateCache.AddOrUpdate(template);
                    }

                    await _logicTemplateFactory.CommitChanges();

                    _logger.LogDebug($"InitRuleTemplates for {factory.LogicName}...done");
                }

                await _dbContext.SaveChangesAsync(true);
            }
            catch (NoManifestFoundException)
            {
                // ignore
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load Rule {factory.LogicName} {e}", e);
            }
            ;
        }
    }
}
