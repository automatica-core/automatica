
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
using Automatica.Core.Runtime.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;
using Automatica.Core.Driver.Monitor;
using Microsoft.Extensions.Configuration;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.IO;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.License;
using Automatica.Core.Internals.Core;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Loader;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;

using Automatica.Core.Internals.Templates;
using Automatica.Core.Runtime.Abstraction;
using Automatica.Core.Runtime.Abstraction.Plugins;
using Automatica.Core.Runtime.Abstraction.Plugins.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Automatica.Core.Runtime.Abstraction.Remote;
using Automatica.Core.Runtime.Core.Plugins;
using Automatica.Core.Runtime.RemoteNode;
using Automatica.Core.Base.Logger;
using Automatica.Core.Control;
using Automatica.Core.Control.Cache;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.RemoteConnect;
using Automatica.Core.Runtime.Recorder.Abstraction;
using Automatica.Core.Base.Calendar;

[assembly: InternalsVisibleTo("Automatica.Core.CI.CreateDatabase")]
[assembly: InternalsVisibleTo("Automatica.Core.WebApi.Tests")]

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
    public class CoreServer : IHostedService, ICoreServer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;
        private readonly IHubContext<DataHub> _dataHub;
        private readonly ILogger _logger;
        private int _configuredDrivers;


        private readonly IDispatcher _dispatcher;
        private readonly ICloudApi _cloudApi;
        private readonly ILicenseContext _licenseContext;
        private readonly ITelegramMonitor _telegramMonitor;
        private readonly ILogicEngineDispatcher _logicEngineDispatcher;
        private RunState _runState;
        private readonly IRuleInstanceVisuNotify _ruleInstanceVisuNotify;
        private readonly ILearnMode _learnMode;

        private readonly IUpdateHandler _updateHandler;

        private readonly IRemoteServerHandler _remoteServerHandler;
        private readonly IRemoteHandler _remoteHandler;

        private readonly IPluginHandler _pluginHandler;
        private readonly IDriverFactoryStore _driverFactoryStore;
        private readonly ILogicFactoryStore _logicFactoryStore;
        private readonly ILoadedStore _store;

        private readonly IDriverLoader _driverLoader;
        private readonly ILogicLoader _logicLoader;

        private readonly IDriverStore _driverStore;
        private readonly ILogicStore _logicStore;
        private readonly ILoadedNodeInstancesStore _loadedNodeInstancesStore;
        private readonly IDriverNodesStoreInternal _driverNodesStoreInternal;
        private readonly IDriverNodesStore _driverNodesStore;
        private readonly ILogicInstancesStore _logicInstanceStore;
        private readonly ILogicTemplateCache _logicTemplateCache;

        private readonly ISettingsCache _settingsCache;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly IDriverFactoryLoader _driverFactoryLoader;
        private readonly ILocalizationProvider _localizationProvider;
        private readonly INodeInstanceService _nodeInstanceService;
        private readonly INodeTemplateCache _nodeTemplateCache;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRemoteConnectService _remoteConnectService;
        private readonly ITrendingContext _trendingContext;
        private readonly INotifyDriver _notifyDriver;
        private readonly ILogicPageCache _logicPageCache;

        private readonly IControlCache _controlCache;
        private readonly IControlContext _controlContext;

        private int _satelliteInstanceCount;

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
            _serviceProvider = services;
            _config = services.GetRequiredService<IConfiguration>();

            _dataHub = services.GetRequiredService<IHubContext<DataHub>>();
            _dispatcher = services.GetRequiredService<IDispatcher>();
            _cloudApi = services.GetRequiredService<ICloudApi>();
            _licenseContext = services.GetRequiredService<ILicenseContext>();

            _logger = services.GetRequiredService<ILogger<CoreServer>>();
            
            _telegramMonitor = services.GetRequiredService<ITelegramMonitor>();
            _ruleInstanceVisuNotify = services.GetRequiredService<IRuleInstanceVisuNotify>();

            _learnMode = services.GetRequiredService<ILearnMode>();

            _remoteServerHandler = services.GetRequiredService<IRemoteServerHandler>();
            _remoteHandler = services.GetRequiredService<IRemoteHandler>();

            _pluginHandler = services.GetRequiredService<IPluginHandler>();
            _updateHandler = services.GetRequiredService<IUpdateHandler>();

            _driverFactoryLoader = services.GetRequiredService<IDriverFactoryLoader>();
            _driverFactoryStore = services.GetRequiredService<IDriverFactoryStore>();
            _logicFactoryStore = services.GetRequiredService<ILogicFactoryStore>();
            _store = services.GetRequiredService<ILoadedStore>();

            _driverLoader = services.GetRequiredService<IDriverLoader>();
            _logicLoader = services.GetRequiredService<ILogicLoader>();

            _driverStore = services.GetRequiredService<IDriverStore>();
            _logicStore = services.GetRequiredService<ILogicStore>();

            _driverNodesStoreInternal = services.GetRequiredService<IDriverNodesStoreInternal>();
            _driverNodesStore = services.GetRequiredService<IDriverNodesStore>();
            _logicInstanceStore = services.GetRequiredService<ILogicInstancesStore>();

            _settingsCache = services.GetRequiredService<ISettingsCache>();
            _nodeInstanceCache = services.GetRequiredService<INodeInstanceCache>();
            _logicInstanceCache = services.GetRequiredService<ILogicInstanceCache>();
            _logicTemplateCache = services.GetRequiredService<ILogicTemplateCache>();

            _loadedNodeInstancesStore = services.GetRequiredService<ILoadedNodeInstancesStore>();

            RunState = RunState.Constructed;

            
            _logicEngineDispatcher = services.GetRequiredService<ILogicEngineDispatcher>();

            _localizationProvider = services.GetRequiredService<ILocalizationProvider>();
            _nodeInstanceService = services.GetRequiredService<INodeInstanceService>();

            _nodeTemplateCache = services.GetRequiredService<INodeTemplateCache>();

            _localizationProvider.LoadFromAssembly(GetType().Assembly);

            _loggerFactory = services.GetRequiredService<ILoggerFactory>();
            _trendingContext = services.GetRequiredService<ITrendingContext>();

            _notifyDriver = services.GetRequiredService<INotifyDriver>();

            _remoteConnectService = services.GetRequiredService<IRemoteConnectService>();
            _logicPageCache = services.GetRequiredService<ILogicPageCache>();

            _controlCache = services.GetRequiredService<IControlCache>();
            _controlContext = services.GetRequiredService<IControlContext>();
            InitInternals();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += (sender, args) =>
            {
                Exception e = (Exception)args.ExceptionObject;
                _logger.LogError(e, $"Unhandled exception occurred {e}");
                Console.Error.WriteLine($"Unhandled exception occurred {e}");
            };
        }

        private void InitInternals()
        {
            _remoteServerHandler.Init();

            _telegramMonitor?.Clear();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var settings = _settingsCache.All().SingleOrDefault(a => a.ValueKey == ServerInfo.DbConfigVersionKey);

            if (settings != null)
            {
                ServerInfo.DbConfigVersion = settings.ValueInt.GetValueOrDefault();

            }
            
            ServerInfo.LoadedConfigVersion = ServerInfo.DbConfigVersion;
            
            Task.Run(async () =>
            {
                try
                {
                    await _pluginHandler.CheckAndInstallPluginUpdates();
                    await _updateHandler.Init();

                    RunState = RunState.Loading;
                    await Load(ServerInfo.PluginDirectory, ServerInfo.PluginFilePattern);
                    await ConfigureAndStart();

                    await _dispatcher.Init(cancellationToken);

                    _logicPageCache.All();

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not startup...");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }


        private async Task ConfigureAndStart()
        {
            if (!await _licenseContext.Init())
            {
                //try again 2nd time - seems buggy, but easy fix for now
                await _licenseContext.Init();
            }

            _satelliteInstanceCount = 0;

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
            

            try
            {
                try
                {
                    await Configure();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error during configuration {e}");
                    throw;
                }
                await StartRemoteConnectService();

                try
                {
                    RunState = RunState.Starting;

                    await StartLogicEngine();
                    foreach (var driver in _driverStore.All())
                    {
                        _logger.LogInformation($"Starting driver {driver.Name}...");
                        await StartDriver(driver);
                    }

                    await StartLogics();
                    await _trendingContext.Start();
                    RunState = RunState.Started;

                    foreach (var driver in _driverStore.All())
                    {
                        await driver.Started();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error during startup {e}");
                    throw;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Some unhandled exception occurred during startup...{e}");
            }
        }

        private async Task StartRemoteConnectService()
        {
            try
            {
                await _remoteConnectService.StartAsync(default);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error starting RemoteControl {e}");
            }

        }

        private async Task StartLogics()
        {
            _logger.LogInformation("Loading logics...");
            foreach (var rule in _logicInstanceStore.Dictionary())
            {
                await StartLogic(rule.Value, rule.Key);
            }
            _logger.LogInformation("Loading logics...done");

        }

        private async Task StartLogicEngine()
        {
            _logger.LogInformation("Loading logic engine connections...");
            await _logicEngineDispatcher.Load();
            _logger.LogInformation("Loading logic engine connections...done");

           
        }
        private async Task StopLogicEngine()
        {
            foreach (var rule in _logicInstanceStore.Dictionary())
            {
                await StopLogic(rule.Value, rule.Key, true);
            }
            await _logicEngineDispatcher.Unload();
        }

        private async Task StartLogic(ILogic logic, RuleInstance logicInstance)
        {
            _logger.LogInformation($"Starting logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...");
            try
            {
                if (await logic.Start())
                {
                    _logicStore.Add(logicInstance, logic);
                    _logger.LogInformation($"Starting logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...success");
                }
                else
                {
                    _logger.LogError($"Starting logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...error");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Starting logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...error {e}");
            }
        }

        private async Task RestartLogic(ILogic logic, RuleInstance logicInstance)
        {
            _logger.LogInformation($"Restarting logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...");
            try
            {
                if (await logic.Restart(logicInstance))
                {
                    _logicStore.Update(logicInstance, logic);
                    _logger.LogInformation($"Restarting logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...success");
                }
                else
                {
                    _logger.LogError($"Restarting logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...error");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Restarting logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...error {e}");
            }
        }

        private async Task StopLogic(ILogic logic, RuleInstance logicInstance, bool unlink)
        {
            try
            {
                _logger.LogInformation($"Stopping logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...");

                if (unlink)
                {
                    foreach (var logicInterface in logicInstance.RuleInterfaceInstance)
                    {
                        await _logicEngineDispatcher.Unlink(logicInterface.ObjId);
                    }
                }

                if (await logic.Stop())
                {
                    _logger.LogInformation($"Stopping logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...success");
                }
                else
                {
                    _logger.LogError($"Stopping logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...error");
                }

                
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Stopping logic {logicInstance.ObjId} {logicInstance.Name} {logicInstance.GetHashCode()}...error");
            }
        }

        public async Task ReloadLogic(Guid ruleInstanceId)
        {
            if (_logicInstanceStore.ContainsRuleInstanceId(ruleInstanceId))
            {
                var oldLogic = _logicInstanceStore.GetByRuleInstanceId(ruleInstanceId);
                await StopLogic(oldLogic.Value, oldLogic.Key, false);
                await oldLogic.Value.Reload();
                var ruleInstance = _logicInstanceCache.Get(ruleInstanceId);
                
                _logicInstanceStore.Update(ruleInstance, oldLogic.Value);
                await RestartLogic(oldLogic.Value, ruleInstance);

            }
            else
            {
                var newLogic = InitLogicInstance(ruleInstanceId);
                await StartLogic(newLogic.Value, newLogic.Key);
            }
        }

        public async Task StopLogic(Guid ruleInstanceId)
        {
            if (!_logicInstanceStore.ContainsRuleInstanceId(ruleInstanceId))
            {
                return;
            }
            var rule = _logicInstanceStore.GetByRuleInstanceId(ruleInstanceId);
            await StopLogic(rule.Value, rule.Key, true);
            
        }

        public Task RemoveLink(Guid linkId)
        {
            return _logicEngineDispatcher.Unlink(linkId);
        }

        public async Task ReloadLinks()
        {
            await _logicEngineDispatcher.Reload();
        }

        public async Task ReloadLogicServices()
        {
            await StopLogicEngine();
            await StartLogicEngine();
            await StartLogics();
        }

        public async Task StopDriver(IDriver driver)
        {
            try
            {

                _licenseContext.DecrementDriverCount(driver.ChildrensCreated);
                _logger.LogInformation($"Stopping driver {driver.Id} {driver.Name}...");

                if (await driver.Stop())
                {
                    _logger.LogInformation($"Stopping driver {driver.Id} {driver.Name}...success");
                }
                else
                {
                    _logger.LogError($"Stopping driver {driver.Id} {driver.Name}...error");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Stopping driver {driver.Id} {driver.Name}...error");
            }

            _driverStore.Remove(driver);
            _driverNodesStoreInternal.RemoveDriver(driver);
        }

        

        private async Task Stop()
        {
            await _remoteServerHandler.Stop();

            foreach (var driver in _driverStore.All())
            {
                await StopDriver(driver);
            }

            await StopLogicEngine();

            foreach (var driver in _driverStore.All())
            {
                await driver.Stopped();
            }


            RunState = RunState.Stopped;
            _logger.LogInformation("CoreServer stopping...");
            IsRunning = false;
            
            _driverStore.Clear();
            _logicStore.Clear();
            
            _driverNodesStoreInternal.Clear();
            _loadedNodeInstancesStore.Clear();
            _logicInstanceStore.Clear();

            _store.Clear();

            await _trendingContext.Stop();

            _logicEngineDispatcher.Dispose();

            await _remoteConnectService.StopAsync(default);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Stop();

            await _remoteConnectService.StopAsync(cancellationToken);

            _logger.LogInformation("CoreServer stopped");
        }


        private async Task ConfigureDriversRecursive(NodeInstance root)
        {
            foreach (var nodeInstance in root.InverseThis2ParentNodeInstanceNavigation)
            {
                _logger.LogDebug($"Working on {nodeInstance.Name}...");
                _loadedNodeInstancesStore.Add(nodeInstance.ObjId, nodeInstance);
               
                if (!nodeInstance.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation.IsDriverInterface)
                {
                    if (LicenseExceeded())
                    {
                        nodeInstance.State = NodeInstanceState.OutOfDataPoints;
                    }
                    else
                    {
                        nodeInstance.State = NodeInstanceState.InUse;
                    }
                    _logger.LogInformation($"Ignoring Non DriverInterface {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}");

                    await ConfigureDriversRecursive(nodeInstance);
                    continue;
                }
                if (nodeInstance.This2NodeTemplateNavigation.IsAdapterInterface != null && nodeInstance.This2NodeTemplateNavigation.IsAdapterInterface.Value)
                {
                    if (LicenseExceeded())
                    {
                        nodeInstance.State = NodeInstanceState.OutOfDataPoints;
                    }
                    else
                    {
                        nodeInstance.State = NodeInstanceState.InUse;
                    }
                    _logger.LogInformation($"Ignoring AdapterInterface {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}");

                    await ConfigureDriversRecursive(nodeInstance);
                    continue;
                }

                if (!_driverFactoryStore.Contains(nodeInstance.This2NodeTemplateNavigation.ObjId))
                {
                    _logger.LogError($"Could not find driver factory for {nodeInstance.This2NodeTemplateNavigation.Name}");
                    continue;
                }

                try
                {
                    if (LicenseExceeded())
                    {
                        _logger.LogError($"Cannot instantiate more data points for driver {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}, license exceeded");
                    }
                    else
                    {
                        _logger.LogDebug($"Creating instance for driver {nodeInstance.Name} - {nodeInstance.This2NodeTemplateNavigation.Key}");
                        await InitializeDriver(nodeInstance, nodeInstance.This2NodeTemplateNavigation);
                    }

                }
                catch (Exception e)
                {
                    _logger.LogDebug($"Could not load driver {e}");
                }
            }
        }

        private void AddRemoteDriverRecursive(Guid driverInstanceGuid, NodeInstance driver)
        {
            driver.State = NodeInstanceState.Remote;

            if (driver.InverseThis2ParentNodeInstanceNavigation == null)
            {
                return;
            }
            foreach (var dr in driver.InverseThis2ParentNodeInstanceNavigation)
            {
                _driverNodesStoreInternal.Add(new RemoteNodeInstance(driverInstanceGuid, dr, _remoteHandler));

                _licenseContext.IncrementDriverCount();
                dr.State = NodeInstanceState.Remote;
                
                if (_licenseContext.DriverLicenseCountExceeded())
                {
                    driver.State = NodeInstanceState.OutOfDataPoints;
                    _logger.LogError("Cannot instantiate more data-points, license exceeded");
                    return; //license will be ignored for now
                }

                AddRemoteDriverRecursive(driverInstanceGuid, dr);
            }
        }

        private bool LicenseExceeded() => _configuredDrivers >= _licenseContext.MaxDataPoints;

        private void AddNodeInstancesRecursive(NodeInstance nodeInstance)
        {
            if (_configuredDrivers >= _licenseContext.MaxDataPoints)
            {
                nodeInstance.State = NodeInstanceState.OutOfDataPoints;
            }
            if(nodeInstance.State == NodeInstanceState.New)
            {
                nodeInstance.State = NodeInstanceState.UnknownError;
            }
            _loadedNodeInstancesStore.Add(nodeInstance.ObjId, nodeInstance);

            if (nodeInstance.InverseThis2ParentNodeInstanceNavigation == null)
            {
                return;
            }

            foreach(var node in nodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                AddNodeInstancesRecursive(node);
            }

        }

        private KeyValuePair<RuleInstance, ILogic> InitLogicInstance(Guid ruleInstanceId)
        {
            var ruleInstance = _logicInstanceCache.Get(ruleInstanceId);
            var factory = _logicFactoryStore.Get(ruleInstance.This2RuleTemplate);

             if (factory == null)
            {
                _logger.LogError($"Could not find logic factory for {ruleInstance.This2RuleTemplateNavigation.Name}");
                throw new ArgumentException("Could not find factory for logic instance..");
            }
            var logger = _loggerFactory.CreateLogger($"{factory.LogicName}{LoggerConstants.FileSeparator}{ruleInstance.ObjId}");
            var ruleContext = new LogicContext(ruleInstance, 
                _dispatcher, 
                new LogicTemplateFactoryProvider(_serviceProvider).CreateInstance(factory.LogicGuid), 
                _ruleInstanceVisuNotify, 
                logger,
                _serviceProvider.GetRequiredService<IServerCloudApi>(), 
                _licenseContext,
                _controlContext,
                DateTimeHelper.ProviderInstance,
                _serviceProvider.GetRequiredService<ILocalizationProvider>());

            var rule = factory.CreateLogicInstance(ruleContext);

            if (rule != null)
            {
                _logicInstanceStore.Add(ruleInstance, rule);
            }

            return new KeyValuePair<RuleInstance, ILogic>(ruleInstance, rule!);
        }


        private async Task Configure()
        {
            _logger.LogDebug("Searching instantiated drivers");
            await _licenseContext.Init();
            if (!_licenseContext.IsLicensed)
            {
                _logger.LogError("Can not configure drivers - license is invalid");
                return;
            }
            _configuredDrivers = 0;

            try
            {
                await _remoteConnectService.InitAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error initialize RemoteControl service {e}");
            }

            var root = _nodeInstanceCache.Root;
            root.State = NodeInstanceState.InUse;
            _loadedNodeInstancesStore.Add(root.ObjId, root);
            await ConfigureDriversRecursive(root);


            var rules = _logicInstanceCache.All();

            foreach (var ruleInstance in rules)
            {
                if (!_logicFactoryStore.Contains(ruleInstance.This2RuleTemplate))
                {
                    _logger.LogWarning($"Could not find LogicFactory for guid {ruleInstance.This2RuleTemplate}");
                    continue;
                }

                try
                {
                    InitLogicInstance(ruleInstance.ObjId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not init logic instance...");
                }
            }

            await _trendingContext.Configure();
        }
        
      

        internal async Task Load(string path, string searchPattern)
        {
            await using var context = new AutomaticaContext(_config);
            try
            {
                _driverFactoryStore.Clear();
                _logicFactoryStore.Clear();

                var driverLoadingPath = Path.Combine(path, ServerInfo.DriversDirectory);

                _logger.LogInformation($"Searching for drivers in {driverLoadingPath}");

                foreach(var plugin in context.Plugins)
                {
                    plugin.Loaded = false;
                    context.Update(plugin);
                }
                await context.SaveChangesAsync();
              
                var foundDrivers = await PluginLoader.GetDriverFactories(_logger, driverLoadingPath, searchPattern, _config, ServerInfo.IsInDevelopmentMode);
                foreach (var driver in foundDrivers)
                {
                    try
                    {
                        await _driverLoader.Load(driver, ServerInfo.BoardType);
                    }
                    catch (NoManifestFoundException)
                    {
                    }
                }

                _nodeTemplateCache.All();
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load drivers {e}", e);
            }

            try
            {
                var ruleLoadingPath = Path.Combine(path, ServerInfo.LogicsDirectory);
                _logger.LogInformation($"Searching for logic's in {ruleLoadingPath}");
                var logicFactories = await RuleLoader.GetRuleFactories(_logger, ruleLoadingPath, searchPattern, _config,
                    ServerInfo.IsInDevelopmentMode);
                foreach (var logic in logicFactories)
                {
                    try
                    {
                        await _logicLoader.Load(logic, ServerInfo.BoardType);
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

            _logicTemplateCache.ClearAndLoad();
            _nodeTemplateCache.ClearAndLoad();
        }

        public async Task ReInit()
        {
            await _remoteServerHandler.ReInit();
            await _driverNodesStoreInternal.ReInitialize();
            _telegramMonitor?.Clear();

            await Stop();
            
            InitInternals();

            await ConfigureAndStart();
            _logger.LogInformation("ReInit done...");

        }

        public async Task<IDriver?> InitializeDriver(NodeInstance nodeInstance, NodeTemplate nodeTemplate)
        {
            if (nodeInstance.This2Slave.HasValue && nodeInstance.This2Slave != ServerInfo.SelfSlaveGuid)
            {
                await _remoteServerHandler.AddNode(nodeInstance.ObjId.ToString(), nodeInstance);
                await _remoteServerHandler.AddSlave(nodeInstance.This2SlaveNavigation.ClientId, _driverFactoryStore.Get(nodeInstance.This2NodeTemplateNavigation.ObjId), nodeInstance);


                _driverNodesStoreInternal.Add(new RemoteNodeInstance(nodeInstance.ObjId, nodeInstance, _remoteHandler));

                if (_satelliteInstanceCount >= _licenseContext.MaxSatellites)
                {
                    nodeInstance.State = NodeInstanceState.OutOfSatelliteLicenses;
                    _logger.LogError($"Can not add more satellites - max count of {_licenseContext.MaxSatellites} reached");
                    return null;
                }

                _satelliteInstanceCount++;
                AddRemoteDriverRecursive(nodeInstance.ObjId, nodeInstance);
                return null;
            }

            var factory = _driverFactoryStore.Get(nodeTemplate.ObjId);
        
            if(factory == null)
            {
                _logger.LogError($"No factory found for {nodeTemplate.Name} ({nodeTemplate.ObjId})");
                return null;
            }
            var manifest = _driverFactoryStore.GetManifestForDriver(factory.DriverGuid);

            if (manifest == null)
            {
                _logger.LogError($"No manifest found for {factory.DriverName} ({factory.DriverGuid})");
                return null;
            }

            var loggerName =
                $"{factory.DriverName.ToLowerInvariant()}{LoggerConstants.FileSeparator}{nodeInstance.Name.Replace(" ", "_").ToLowerInvariant()}";
            var logger = _loggerFactory.CreateLogger(loggerName);
            _logger.LogInformation($"Using logger {loggerName} for driver {nodeInstance.Name}");

            var config = new DriverContext(
                nodeInstance, 
                factory,
                _dispatcher, 
               new NodeTemplateFactoryProvider(_serviceProvider).CreateInstance(manifest.Automatica.PluginGuid),
                _telegramMonitor, 
                _licenseContext.GetLicenseState(), 
                logger, 
                _learnMode,
                _serviceProvider.GetRequiredService<IServerCloudApi>(), 
                _licenseContext, 
                _loggerFactory, 
                _serviceProvider,
                _controlContext,
                DateTimeHelper.ProviderInstance,
                false);

            var driver = await _driverFactoryLoader.LoadDriverFactory(nodeInstance, factory, config);

            AddNodeInstancesRecursive(nodeInstance);

            return driver;

        }

        public async Task StartStopDriver(NodeInstance nodeInstance)
        {
            var rootNode = _nodeInstanceCache.GetDriverNodeInstanceFromChild(nodeInstance);
            await _notifyDriver.NotifyAdd(nodeInstance);
            if (rootNode == null)
            {
                return;
            }
            var driver = _driverNodesStore.GetDriver(rootNode.ObjId);
            if (driver == null)
            {
                _logger.LogWarning(
                    $"Could not hot-reload driver, seems that the driver wasn't loaded at the moment");

                return;
            }

            await StopDriver(driver);
            await InitializeAndStartDriver(rootNode, _nodeTemplateCache.Get(rootNode!.This2NodeTemplate!.Value));
        }

        public async Task InitializeAndStartDriver(NodeInstance nodeInstance, NodeTemplate nodeTemplate)
        {
            var driver = await InitializeDriver(nodeInstance, nodeTemplate);
            if (driver == null)
            {
                _logger.LogError($"Could not initialize driver for {nodeInstance.Name}");
                return;
            }
            await StartDriver(driver);
            await driver.Started();
        }

        private async Task StartDriver(IDriver driver)
        {
            try
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(120));

                bool driverStart;
                if (driver is DriverBase driverBase)
                {
                    driverStart = await driverBase.StartInternal(cts.Token);
                }
                else
                {
                    driverStart = await driver.Start(cts.Token);
                }

                if (driverStart)
                {
                    if (driver.DriverContext.NodeInstance.State == NodeInstanceState.Initialized)
                    {
                        driver.DriverContext.NodeInstance.State = NodeInstanceState.InUse;
                    }
                    _logger.LogInformation($"Starting driver {driver.Id} {driver.Name}...done");
                }
                else
                {
                    driver.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                    driver.DriverContext.NodeInstance.Error = driver.Error;
                    _logger.LogError($"Could not start driver {driver.Id} {driver.Name}");
                }
            }
            catch (OperationCanceledException canceled)
            {
                driver.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                driver.DriverContext.NodeInstance.Error = canceled.ToString();
                _logger.LogError(canceled, $"Could not start driver {driver.Id} {driver.Name}. Task was canceled after 30seconds");
            }
            catch (Exception e)
            {
                driver.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                driver.DriverContext.NodeInstance.Error = e.ToString();
                _logger.LogError(e, $"Could not start driver {driver.Id} {driver.Name}");
            }
        }

        public void Restart()
        {
            Environment.Exit(ServerInfo.ExitCodePluginUpdateInstall);
        }
    }
}
