using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using System;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Localization;
using Automatica.Core.EF.Helper;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.Runtime.Exceptions;
using Microsoft.Extensions.Configuration;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.IO;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.License;
using Automatica.Core.Internals.Core;
using Automatica.Core.Base.Extensions;
using Automatica.Core.Common.Update;
using System.Reflection;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Logger;
using Automatica.Core.Internals.Templates;

[assembly: InternalsVisibleTo("Automatica.Core.CI.CreateDatabase")]

namespace Automatica.Core.Runtime.Core
{
    public enum RunState
    {
        Constructed,
        Loading,
        Configure,
        Starting,
        Started,
        Stopped
    }
    public class CoreServer : INotifyDriver, IRuleVisualisation, IHostedService, ICoreServer
    {
        private AutomaticaContext _dbContext;
        private readonly IConfiguration _config;
        private readonly IHubContext<DataHub> _dataHub;
        private readonly ILogger _logger;
        private int _configuredDrivers;

        private readonly IDictionary<Guid, IDriverFactory> _driverFactories;
        private readonly IList<IDriver> _driverInstances = new List<IDriver>();
        private readonly IList<IDriverNode> _driverNodes = new List<IDriverNode>();
        private readonly IDictionary<Guid, IDriverNode> _driverNodesMap = new Dictionary<Guid, IDriverNode>();
        
        private readonly IDictionary<Guid, NodeInstance> _loadedNodeInstances = new Dictionary<Guid, NodeInstance>();
        
        private readonly IDictionary<Guid, IRuleFactory> _ruleFactories = new Dictionary<Guid, IRuleFactory>();
        private readonly IDictionary<RuleInstance, IRule> _ruleInstances = new Dictionary<RuleInstance, IRule>();
        private readonly IDictionary<Guid, IRule> _ruleIdInstances = new Dictionary<Guid, IRule>();

        private readonly IDictionary<Guid, PluginManifest> _loadedPlugins = new Dictionary<Guid, PluginManifest>();

        private readonly IDispatcher _dispatcher;
        private readonly ICloudApi _cloudApi;
        private readonly ILicenseContext _licenseContext;
        private readonly ITelegramMonitor _telegramMonitor;
        private RuleEngineDispatcher _ruleEngineDispatcher;
        private RunState _runState;
        private readonly ILocalizationProvider _localizationProvider;
        private readonly IRuleInstanceVisuNotify _ruleInstanceVisuNotify;
        private readonly ILearnMode _learnMode;

        private readonly AutomaticUpdate _automaticUpdate;

        public IList<IDriverNode> DriverNodes => _driverNodes;
        public IDictionary<RuleInstance, IRule> Rules => _ruleInstances;

        public RunState RunState
        {
            get => _runState;
            set
            {
                _runState = value;
                _dataHub?.Clients.All.SendAsync("serverStateChanged", RunState );
            }
        }

        public bool IsRunning { get; private set; } = true;


        public CoreServer(IServiceProvider services)
        {
            _config = services.GetService<IConfiguration>();

            _dataHub = services.GetService<IHubContext<DataHub>>();
            _dispatcher = services.GetService<IDispatcher>();

            _cloudApi = services.GetService<ICloudApi>();

            _licenseContext = services.GetService<ILicenseContext>();

            InitInternals();
            _logger = SystemLogger.Instance;
            _driverFactories = new Dictionary<Guid, IDriverFactory>();
            

            _telegramMonitor = services.GetService<ITelegramMonitor>();

            
            _ruleInstanceVisuNotify = new RuleInstanceVisuNotifier(_dataHub);

            _localizationProvider = services.GetService(typeof(LocalizationProvider)) as LocalizationProvider;

            _learnMode = services.GetRequiredService<ILearnMode>();

            _automaticUpdate = new AutomaticUpdate(this, _config, _cloudApi);
            RunState = RunState.Constructed;
        }

        private void InitInternals()
        {
            _telegramMonitor?.Clear();
            _dbContext = new AutomaticaContext(_config);
            _ruleEngineDispatcher = new RuleEngineDispatcher(_dbContext, this, _dispatcher);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var settings = _dbContext.Settings.SingleOrDefault(a => a.ValueKey == ServerInfo.DbConfigVersionKey);

            if (settings != null)
            {
                ServerInfo.DbConfigVersion = settings.ValueInt.GetValueOrDefault();

            }
            
            ServerInfo.LoadedConfigVersion = ServerInfo.DbConfigVersion;
            
            await Task.Run(async () =>
            {
                CheckAndInstallPluginUpdates();

                _automaticUpdate.Init();

                RunState = RunState.Loading;
                Load(ServerInfo.DriverDirectoy, ServerInfo.DriverPattern);
                await ConfigureAndStart();
            }, cancellationToken);
        }

        private async Task ConfigureAndStart()
        {
            await _licenseContext.Init();

            RunState = RunState.Configure;
            ServerInfo.IsConnectedToCloud = await _cloudApi.Ping();

#pragma warning disable 4014
            //should run async and not awaited!
            Task.Run(async () =>
            {
                //say hello to cloud
                if (ServerInfo.IsConnectedToCloud)
                {
                    await _cloudApi.SayHelloToCloud(new SayHelloData()
                    {
                        Rid = ServerInfo.Rid,
                        ServerGuid = ServerInfo.ServerUid,
                        Version = ServerInfo.GetServerVersion()
                    });
                }

            });
#pragma warning restore 4014
           

            Configure();

            RunState = RunState.Starting;

            foreach (var driver in _driverInstances)
            {
                _logger.LogInformation($"Starting driver {driver.Name}...");
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(120));
                    var driverStart = await driver.Start().WithCancellation(cts.Token);

                    if (driverStart)
                    {
                        if (driver.DriverContext.NodeInstance.State == NodeInstanceState.Initialized)
                        {
                            driver.DriverContext.NodeInstance.State = NodeInstanceState.InUse;
                        }
                        _logger.LogInformation($"Starting driver {driver.Name}...done");
                    }
                    else
                    {
                        driver.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                        _logger.LogError($"Could not start driver {driver.Name}");
                    }
                }
                catch (OperationCanceledException canceled)
                {
                    driver.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                    _logger.LogError(canceled, $"Could not start driver {driver.Name}. Task was canceled after 30seconds");
                }
                catch (Exception e)
                {
                    driver.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                    _logger.LogError(e, $"Could not start driver {driver.Name}");
                }
            }


            _logger.LogInformation("Loading logic engine connections...");
            _ruleEngineDispatcher.Load();
            _logger.LogInformation("Loading logic engine connections...done");

            foreach (var rule in _ruleInstances)
            {
                _logger.LogInformation($"Starting logic {rule.Key.Name}...");

                if (await rule.Value.Start())
                {
                    _logger.LogInformation($"Starting logic {rule.Key.Name}...success");
                }
                else
                {
                    _logger.LogError($"Starting logic {rule.Key.Name}...error");
                }
            }

            RunState = RunState.Started;
        }

        private void CheckAndInstallPluginUpdates()
        {
            var updateDirectory = Path.Combine(Path.GetTempPath(), ServerInfo.PluginUpdateDirectoryName);
            if (!Directory.Exists(updateDirectory))
            {
                Directory.CreateDirectory(updateDirectory);
                return;
            }

            var files = Directory.GetFiles(updateDirectory);

            foreach(var f in files)
            {
                try
                {
                    var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", ""));
                    var manifest = Common.Update.Plugin.GetPluginManifest(_logger, f, tmp);

                    Directory.Delete(tmp, true);
                    if (manifest == null)
                    {
                        _logger.LogWarning($"Could no update plugin with file from {f}");

                        File.Delete(f);
                        continue;
                    }

                    var assemblyDir = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
                    var pluginType = manifest.Automatica.Type == "driver" ? ServerInfo.DriversDirectory : ServerInfo.LogicsDirectory;
                    var pluginDir = Path.Combine(assemblyDir, pluginType);
                    var componentDir = Path.Combine(pluginDir, manifest.Automatica.ComponentName);

                    if (Directory.Exists(componentDir))
                    {
                        Directory.Delete(componentDir, true);
                    }
                    Common.Update.Plugin.InstallPlugin(f, pluginDir);
                }
                catch(Exception e)
                {
                    _logger.LogError(e, "Could not install plugin");
                }
                File.Delete(f);
            }
        }

        private async Task Stop()
        {
            foreach (var driver in _driverInstances)
            {
                _logger.LogInformation($"Stoping driver {driver.Name}...");
                if (await driver.Stop())
                {
                    _logger.LogInformation($"Stoping driver {driver.Name}...success");
                }
                else
                {
                    _logger.LogError($"Stoping driver {driver.Name}...error");
                }
            }

            foreach (var rule in _ruleInstances)
            {
                _logger.LogInformation($"Stoping logic {rule.Key.Name}...");

                if (await rule.Value.Stop())
                {
                    _logger.LogInformation($"Stoping logic {rule.Key.Name}...success");
                }
                else
                {
                    _logger.LogError($"Stoping logic {rule.Key.Name}...error");
                }
            }

            RunState = RunState.Stopped;
            _logger.LogInformation("CoreServer stopping...");
            IsRunning = false;

            _driverInstances.Clear();
            _ruleInstances.Clear();
            _ruleIdInstances.Clear();
            _driverNodesMap.Clear();
            _loadedNodeInstances.Clear();
            _loadedPlugins.Clear();

            _dbContext.Dispose();
            _ruleEngineDispatcher.Dispose();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Stop();
            _logger.LogInformation("CoreServer stopped");
        }


        private void ConfigureDriversRecursive(NodeInstance root)
        {
            foreach (var nodeInstance in root.InverseThis2ParentNodeInstanceNavigation)
            {
                _logger.LogDebug($"Working on {nodeInstance.Name}...");
                _loadedNodeInstances.Add(nodeInstance.ObjId, nodeInstance);
                if (!nodeInstance.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation.IsDriverInterface)
                {
                    if (LicenseExceeded())
                    {
                        nodeInstance.State = NodeInstanceState.OutOfDatapoits;
                    }
                    else
                    {
                        nodeInstance.State = NodeInstanceState.InUse;
                    }
                    _logger.LogDebug($"Ignoring Non DriverInterface {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}");

                    ConfigureDriversRecursive(nodeInstance);
                    continue;
                }
                if (nodeInstance.This2NodeTemplateNavigation.IsAdapterInterface != null && nodeInstance.This2NodeTemplateNavigation.IsAdapterInterface.Value)
                {
                    if (LicenseExceeded())
                    {
                        nodeInstance.State = NodeInstanceState.OutOfDatapoits;
                    }
                    else
                    {
                        nodeInstance.State = NodeInstanceState.InUse;
                    }
                    _logger.LogDebug($"Ignoring AdapterInterface {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}");

                    ConfigureDriversRecursive(nodeInstance);
                    continue;
                }

                if (!_driverFactories.ContainsKey(nodeInstance.This2NodeTemplateNavigation.ObjId))
                {
                    nodeInstance.State = NodeInstanceState.UnknownError;
                    _logger.LogDebug($"Could not find factory for driver {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}");
                    continue;
                }

                try
                {
                    if (LicenseExceeded())
                    {
                        _logger.LogError($"Cannot instantiate more datapoints for driver {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}, license exceeded");
                    }
                    else
                    {
                        _logger.LogDebug($"Creating instance for driver {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}");

                        var factory = _driverFactories[nodeInstance.This2NodeTemplateNavigation.ObjId];
                        var config = new DriverContext(NodeInstanceHelper.RecursiveLoad(nodeInstance, _dbContext),
                        _dispatcher, new NodeTemplateFactory(new AutomaticaContext(_config), _config), _telegramMonitor, _licenseContext.GetLicenseState(), CoreLoggerFactory.GetLogger(factory.DriverName), _learnMode, _cloudApi, _licenseContext, false);
                        var driver = factory.CreateDriver(config);

                        nodeInstance.State = NodeInstanceState.Loaded;
                        if (driver.BeforeInit())
                        {
                            _driverInstances.Add(driver);
                            nodeInstance.State = NodeInstanceState.Initialized;
                            driver.Configure();
                        }
                        else
                        {
                            nodeInstance.State = NodeInstanceState.UnknownError;
                        }

                        _driverNodes.Add(driver);
                        _driverNodesMap.Add(driver.Id, driver);
                        _configuredDrivers += 1 + driver.ChildrensCreated;

                        AddDriverRecurisve(driver);
                    }

                    AddNodeInstancesRecursive(nodeInstance);
                }
                catch (Exception e)
                {
                    _logger.LogDebug($"Could not load driver {e}");
                }
            }
        }

        private bool LicenseExceeded() => _configuredDrivers >= _licenseContext.MaxDatapoints;

        private void AddNodeInstancesRecursive(NodeInstance nodeInstance)
        {
            if (_configuredDrivers >= _licenseContext.MaxDatapoints)
            {
                nodeInstance.State = NodeInstanceState.OutOfDatapoits;
            }
            if(nodeInstance.State == NodeInstanceState.New)
            {
                nodeInstance.State = NodeInstanceState.UnknownError;
            }
            if (!_loadedNodeInstances.ContainsKey(nodeInstance.ObjId))
            {
                _loadedNodeInstances.Add(nodeInstance.ObjId, nodeInstance);
            }

            if (nodeInstance.InverseThis2ParentNodeInstanceNavigation == null)
            {
                return;
            }

            foreach(var node in nodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                AddNodeInstancesRecursive(node);
            }

        }


        private void Configure()
        {
            _logger.LogDebug("Searching instantiated drivers");

            //if(!_licenseContext.IsLicensed)
            //{
            //    _logger.LogError("Can not configure drivers - license is invalid");
            //    return;
            //}
            _configuredDrivers = 0;

            var root = _dbContext.NodeInstances.Single(a => a.This2ParentNodeInstance == null && !a.IsDeleted);

            root.InverseThis2ParentNodeInstanceNavigation.Add(NodeInstanceHelper.RecursiveLoad(root, _dbContext));
            root.State = NodeInstanceState.InUse;
            _loadedNodeInstances.Add(root.ObjId, root);
            ConfigureDriversRecursive(root);


            var rules = _dbContext.RuleInstances.Include(a => a.RuleInterfaceInstance)
                .ThenInclude(a => a.This2RuleInterfaceTemplateNavigation).Include(a => a.This2RuleTemplateNavigation);

            foreach (var ruleInstance in rules)
            {
                if (!_ruleFactories.ContainsKey(ruleInstance.This2RuleTemplate))
                {
                    _logger.LogWarning($"Could not find RuleFactory for guid {ruleInstance.This2RuleTemplate}");
                    continue;
                }

                _dbContext.Entry(ruleInstance).Reload();

                foreach (var inter in ruleInstance.RuleInterfaceInstance)
                {
                    _dbContext.Entry(inter).Reload();
                }

                var factory = _ruleFactories[ruleInstance.This2RuleTemplate];
                var ruleContext = new RuleContext(ruleInstance, _dispatcher, _ruleInstanceVisuNotify, CoreLoggerFactory.GetLogger(factory.RuleName), _cloudApi, _licenseContext);
                var rule = factory.CreateRuleInstance(ruleContext);

                if (rule != null)
                {
                    _ruleInstances.Add(ruleInstance, rule);
                    _ruleIdInstances.Add(ruleInstance.ObjId, rule);
                }
            }
        }
        
        private void AddDriverRecurisve(IDriverNode driver)
        {
            if (driver.Children == null)
            {
                return;
            }
            foreach (var dr in driver.Children)
            {
                _driverNodes.Add(dr);
                if (!_driverNodesMap.ContainsKey(dr.Id))
                    _driverNodesMap.Add(dr.Id, dr);

                _configuredDrivers++;

                if (_configuredDrivers >= _licenseContext.MaxDatapoints)
                {
                    _logger.LogError("Cannot instantiate more datapoints, license exceeded");
                    return;
                }

                AddDriverRecurisve(dr);
            }
        }

        internal void Load(string path, string searchPattern)
        {
            try
            {
                _driverFactories.Clear();
                _ruleFactories.Clear();

                var driverLoadingPath = Path.Combine(path, "Drivers");

                _logger.LogInformation($"Searching for drivers in {driverLoadingPath}");

                foreach(var plugin in _dbContext.Plugins)
                {
                    plugin.Loaded = false;
                    _dbContext.Update(plugin);
                }
                _dbContext.SaveChanges();
              
                var foundDrivers = DriverLoader.GetDriverFactories(_logger, driverLoadingPath, searchPattern, _dbContext, ServerInfo.IsInDevelopmentMode);
                foreach (var driver in foundDrivers)
                {
                    try
                    {
                        InitDriverFactory(driver);
                    }
                    catch (NoManifestFoundException)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load drivers {e}", e);
            }

            try
            {
                var ruleLoadingPath = Path.Combine(path, "Rules");
                _logger.LogInformation($"Searching for logics in {ruleLoadingPath}");
                foreach (var rule in RuleLoader.GetRuleFactories(_logger, ruleLoadingPath, searchPattern, _dbContext, ServerInfo.IsInDevelopmentMode))
                {
                    try
                    {
                        InitRuleFactory(rule);
                    }
                    catch (NoManifestFoundException)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load rules {e}", e);
            }
            _dbContext.SaveChanges(true);
        }

        private void InitDriverFactory(IDriverFactory driver)
        {
            try
            {
                var manifest = Common.Update.Plugin.GetEmbeddedPluginManifest(_logger, driver.GetType().Assembly);

                if (manifest == null)
                {
                    throw new NoManifestFoundException();
                }
                if (!_loadedPlugins.ContainsKey(manifest.Automatica.PluginGuid))
                {
                    _loadedPlugins.Add(manifest.Automatica.PluginGuid, manifest);
                }
                
                _driverFactories.Add(driver.DriverGuid, driver);
                _logger.LogDebug($"Init driver {driver.DriverName} {driver.DriverVersion}...");

                var driverDbVersion =
                    _dbContext.VersionInformations.SingleOrDefault(a => a.DriverGuid == driver.DriverGuid);
                var initNodeTemplates = false;

                if (driverDbVersion == null)
                {
                    driverDbVersion = new VersionInformation
                    {
                        Name = driver.DriverName,
                        Version = driver.DriverVersion.ToString(),
                        DriverGuid = driver.DriverGuid
                    };
                    initNodeTemplates = true;
                    _dbContext.VersionInformations.Add(driverDbVersion);
                }
                else if (driver.DriverVersion > driverDbVersion.VersionData)
                {
                    initNodeTemplates = true;
                    driverDbVersion.Name = driver.DriverName;
                    driverDbVersion.Version = driver.DriverVersion.ToString();
                }

                _localizationProvider.LoadFromAssembly(driver.GetType().Assembly);
                if (initNodeTemplates || driver.InDevelopmentMode)
                {
                    _logger.LogDebug($"InitNodeTemplates for {driver.DriverName}...");
                    using (var db = new AutomaticaContext(_config))
                    {
                        driver.InitNodeTemplates(new NodeTemplateFactory(db, _config));
                        db.SaveChanges();
                    }
                    _logger.LogDebug($"InitNodeTemplates for {driver.DriverName}...done");
                }
                else
                {
                    driver.InitNodeTemplates(new DoNothingNodeTemplateFactory());
                }
                _logger.LogDebug($"Init driver {driver.DriverName} {driver.DriverVersion}...done");

                _dbContext.SaveChanges();

            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load driver {driver.DriverName} {e}", e);
            }
        }

        private void InitRuleFactory(IRuleFactory rule)
        {
            try
            {
                var manifest = Common.Update.Plugin.GetEmbeddedPluginManifest(_logger, rule.GetType().Assembly);

                if (manifest == null)
                {
                    throw new NoManifestFoundException();
                }

                if (!_loadedPlugins.ContainsKey(manifest.Automatica.PluginGuid))
                {
                    _loadedPlugins.Add(manifest.Automatica.PluginGuid, manifest);
                }

                _ruleFactories.Add(rule.RuleGuid, rule);
                _logger.LogDebug($"Init logic {rule.RuleName} {rule.RuleVersion}...");

                var driverDbVersion =
                    _dbContext.VersionInformations.SingleOrDefault(a => a.RuleGuid == rule.RuleGuid);
                var initNodeTemplates = false;

                if (driverDbVersion == null)
                {
                    driverDbVersion = new VersionInformation
                    {
                        Name = rule.RuleName,
                        Version = rule.RuleVersion.ToString(),
                        RuleGuid = rule.RuleGuid
                    };
                    initNodeTemplates = true;
                    _dbContext.VersionInformations.Add(driverDbVersion);
                }
                else if (rule.RuleVersion > driverDbVersion.VersionData)
                {
                    initNodeTemplates = true;
                    driverDbVersion.Name = rule.RuleName;
                    driverDbVersion.Version = rule.RuleVersion.ToString();
                }

                _localizationProvider.LoadFromAssembly(rule.GetType().Assembly);
                if (initNodeTemplates || rule.InDevelopmentMode)
                {
                    _logger.LogDebug($"InitRuleTemplates for {rule.RuleName}...");
                    
                    using (var db = new AutomaticaContext(_config))
                    {
                        rule.InitTemplates(new RuleTemplateFactory(db, _config));
                        db.SaveChanges();
                    }
                    _logger.LogDebug($"InitRuleTemplates for {rule.RuleName}...done");
                }

                _dbContext.SaveChanges(true);
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load Rule {rule.RuleName} {e}", e);
            }
        }

        public Task NotifySave(NodeInstance node)
        {

            try
            {
                if (_driverNodesMap.ContainsKey(node.ObjId))
                {
                    return _driverNodesMap[node.ObjId].OnSave(node);

                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not notify save {e}");
            }

            return Task.CompletedTask;
        }

        public Task NotifyDeleted(NodeInstance node)
        {
            if (_driverNodesMap.ContainsKey(node.ObjId))
            {
                return _driverNodesMap[node.ObjId].OnDelete(node);
            }

            return Task.CompletedTask;
        }

        public Task<IList<NodeInstance>> ScanBus(NodeInstance node)
        {
            if (_driverNodesMap.ContainsKey(node.ObjId))
            {
                return _driverNodesMap[node.ObjId].Scan();
            }

            throw new NodeNotFoundException();
        }

        public Task<IList<NodeInstance>> CustomAction(NodeInstance node, string actionName)
        {
            if (_driverNodesMap.ContainsKey(node.ObjId))
            {
                return _driverNodesMap[node.ObjId].CustomAction(actionName);
            }

            throw new NodeNotFoundException();
        }

        public Task<bool> EnableLearnMode(NodeInstance node)
        {
            _logger.LogDebug($"Enable learn mode for {node.Name}");
            if (_driverNodesMap.ContainsKey(node.ObjId))
            {
                return _driverNodesMap[node.ObjId].EnableLearnMode();
            }

            throw new NodeNotFoundException();
        }

        public Task<bool> DisableLearnMode(NodeInstance node)
        {
            _logger.LogDebug($"Disable learn mode for {node.Name}");
            if (_driverNodesMap.ContainsKey(node.ObjId))
            {
                return _driverNodesMap[node.ObjId].DisableLearnMode();
            }

            throw new NodeNotFoundException();
        }

        public Task<bool> Read(NodeInstance node)
        {
            if (_driverNodesMap.ContainsKey(node.ObjId))
            {
                return _driverNodesMap[node.ObjId].Read();
            }

            throw new NodeNotFoundException();

        }

        public Task<IList<NodeInstance>> Import(NodeInstance node, string fileName)
        {
            if (_driverNodesMap.ContainsKey(node.ObjId))
            {
                return _driverNodesMap[node.ObjId].Import(fileName);
            }
            throw new NodeNotFoundException();
        }

        public async Task Reinit()
        {
            foreach (var driver in _driverNodesMap.Values)
            {
                await driver.OnReinit();
            }

            await Stop();

            InitInternals();

            await ConfigureAndStart();
            _logger.LogInformation("Reinit done...");

        }

        public object GetDataForRuleInstance(Guid id)
        {
            if (_ruleIdInstances.ContainsKey(id))
            {
                return _ruleIdInstances[id].GetDataForVisu();
            }
            throw new RuleNotFoundException();
        }

        public void Update()
        {
            Environment.Exit(ServerInfo.ExitCodeUpdateInstall);
        }

        public void ReinitAutomaticUpdate()
        {
            _automaticUpdate.Init();
        }

        public NodeInstanceState GetNodeInstanceState(Guid id)
        {
            if (_loadedNodeInstances.ContainsKey(id))
            {
                return _loadedNodeInstances[id].State;
            }
            return NodeInstanceState.Unknown;
        }

        public IList<PluginManifest> GetLoadedPlugins()
        {
            return _loadedPlugins.Values.ToList();
        }

        public void LoadPlugin(EF.Models.Plugin plugin)
        {
            lock (_cloudApi)
            {
                if (plugin.PluginType == PluginType.Driver)
                {
                    var factories = DriverLoader.LoadSingle(_logger, plugin, _dbContext);

                    foreach (var factory in factories)
                    {
                        InitDriverFactory(factory);
                    }
                }
                else if (plugin.PluginType == PluginType.Logic)
                {
                    var factories = RuleLoader.LoadSingle(_logger, plugin, _dbContext);

                    foreach (var factory in factories)
                    {
                        InitRuleFactory(factory);
                    }
                }
            }
        }

        public void Restart()
        {
            Environment.Exit(ServerInfo.ExitCodePluginUpdateInstall);
        }
    }
}
