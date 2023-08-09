using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.BoardType;
using Automatica.Core.Base.Localization;
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

        public LogicLoader(ILogger<LogicLoader> logger, AutomaticaContext dbContext, ILocalizationProvider localizationProvider, IConfiguration config, ILoadedStore store, ILogicFactoryStore logicFactoryStore, ILogicTemplateCache logicTemplateCache)
        {
            _logger = logger;
            _dbContext = dbContext;
            _localizationProvider = localizationProvider;
            _config = config;
            _store = store;
            _logicFactoryStore = logicFactoryStore;
            _logicTemplateCache = logicTemplateCache;
        }

        public Task Load(ILogicFactory factory, IBoardType boardType)
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
                    _logger.LogDebug($"InitRuleTemplates for {factory.LogicName}...");

                    using (var db = new AutomaticaContext(_config))
                    {
                        var logicTemplateFactory = new LogicTemplateFactory(_logger, db, _config, factory);
                        factory.InitTemplates(logicTemplateFactory);

                        foreach (var template in logicTemplateFactory.LogicTemplates.Values)
                        {
                            _logicTemplateCache.AddOrUpdate(template);
                        }

                        db.SaveChanges();
                    }
                    _logger.LogDebug($"InitRuleTemplates for {factory.LogicName}...done");
                }

                _dbContext.SaveChanges(true);
            }
            catch (NoManifestFoundException)
            {
                // ignore
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load Rule {factory.LogicName} {e}", e);
            }
            return Task.CompletedTask;
            ;
        }
    }
}
