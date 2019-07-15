using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Localization;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.Abstraction.Plugins;
using Automatica.Core.Runtime.Abstraction.Plugins.Logics;
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

        public LogicLoader(ILogger<LogicLoader> logger, AutomaticaContext dbContext, ILocalizationProvider localizationProvider, IConfiguration config, ILoadedStore store, ILogicFactoryStore logicFactoryStore)
        {
            _logger = logger;
            _dbContext = dbContext;
            _localizationProvider = localizationProvider;
            _config = config;
            _store = store;
            _logicFactoryStore = logicFactoryStore;
        }

        public Task Load(IRuleFactory factory)
        {
            try
            {
                var manifest = Common.Update.Plugin.GetEmbeddedPluginManifest(_logger, factory.GetType().Assembly);

                if (manifest == null)
                {
                    throw new NoManifestFoundException();
                }


                _store.Add(manifest.Automatica.PluginGuid, manifest);

                _logicFactoryStore.Add(factory.RuleGuid, factory);
                _logger.LogDebug($"Init logic {factory.RuleName} {factory.RuleVersion}...");

                var driverDbVersion =
                    _dbContext.VersionInformations.SingleOrDefault(a => a.RuleGuid == factory.RuleGuid);
                var initNodeTemplates = false;

                if (driverDbVersion == null)
                {
                    driverDbVersion = new VersionInformation
                    {
                        Name = factory.RuleName,
                        Version = factory.RuleVersion.ToString(),
                        RuleGuid = factory.RuleGuid
                    };
                    initNodeTemplates = true;
                    _dbContext.VersionInformations.Add(driverDbVersion);
                }
                else if (factory.RuleVersion > driverDbVersion.VersionData)
                {
                    initNodeTemplates = true;
                    driverDbVersion.Name = factory.RuleName;
                    driverDbVersion.Version = factory.RuleVersion.ToString();
                }

                _localizationProvider.LoadFromAssembly(factory.GetType().Assembly);
                if (initNodeTemplates || factory.InDevelopmentMode)
                {
                    _logger.LogDebug($"InitRuleTemplates for {factory.RuleName}...");

                    using (var db = new AutomaticaContext(_config))
                    {
                        factory.InitTemplates(new RuleTemplateFactory(db, _config));
                        db.SaveChanges();
                    }
                    _logger.LogDebug($"InitRuleTemplates for {factory.RuleName}...done");
                }

                _dbContext.SaveChanges(true);
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load Rule {factory.RuleName} {e}", e);
            }
            return Task.CompletedTask;
            ;
        }
    }
}
