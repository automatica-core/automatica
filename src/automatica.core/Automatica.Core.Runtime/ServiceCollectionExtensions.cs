using System.Net;
using Automatica.Core.Base;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Visu;
using Automatica.Core.Driver;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.License;
using Automatica.Core.Internals.Plugins;
using Automatica.Core.Runtime.Abstraction;
using Automatica.Core.Runtime.Abstraction.Plugins;
using Automatica.Core.Runtime.Abstraction.Plugins.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Automatica.Core.Runtime.Abstraction.Remote;
using Automatica.Core.Runtime.Core;
using Automatica.Core.Runtime.Core.Plugins;
using Automatica.Core.Runtime.Core.Plugins.Drivers;
using Automatica.Core.Runtime.Core.Plugins.Logics;
using Automatica.Core.Runtime.Core.Update;
using Automatica.Core.Runtime.Database;
using Automatica.Core.Runtime.IO;
using Automatica.Core.Visu;
using Automatica.Push;
using Automatica.Push.Concurrency;
using Automatica.Push.Hubs;
using Automatica.Push.LearnMode;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

namespace Automatica.Core.Runtime
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAutomaticaCoreService(this IServiceCollection services, IConfiguration configuration, bool isElectronActive)
        {
            services.AddAutomaticaDrivers();
            services.AddDispatcher();

            services.AddSingleton<ILocalizationProvider>(new LocalizationProvider(SystemLogger.Instance));

            services.AddSingleton<IVisualisationFactory, VisuTempInit>();
            services.AddSingleton<ITelegramMonitor, TelegramMonitor>();
            services.AddSingleton<IServerCloudApi, CloudApi>();
            services.AddSingleton<ICloudApi, CloudApi>();
            services.AddSingleton<ILicenseContext, AllowAllLicenseContext>();
            services.AddSingleton<ILicenseContract>(provider => provider.GetService<ILicenseContext>());
            services.AddSingleton<ILearnMode, LearnMode>();
            services.AddAutomaticaPushServices(configuration, isElectronActive);

            if (!isElectronActive)
            {
                services.AddSingleton<IDriverNodesStoreInternal, DriverNodesStoreInternal>();
                services.AddSingleton<INodeInstanceStore, NodeInstanceStore>();
                services.AddSingleton<ILogicInstancesStore, LogicInstanceStore>();

                services.AddSingleton<LogicStore, LogicStore>();
                services.AddSingleton<ILogicStore>(a => a.GetService<LogicStore>());
                services.AddSingleton<IRuleDataHandler>(a => a.GetService<LogicStore>());

                services.AddSingleton<LoadedNodeInstancesStore, LoadedNodeInstancesStore>();
                services.AddSingleton<INodeInstanceStateHandler>(a => a.GetService<LoadedNodeInstancesStore>());
                services.AddSingleton<ILoadedNodeInstancesStore>(a => a.GetService<LoadedNodeInstancesStore>());

                services.AddSingleton<NativeUpdateHandler, NativeUpdateHandler>();
                services.AddSingleton<IUpdateHandler>(a => a.GetService<NativeUpdateHandler>());
                services.AddSingleton<IAutoUpdateHandler>(a => a.GetService<NativeUpdateHandler>());

                services.AddSingleton<ILoadedStore, LoadedStore>();
                services.AddSingleton<ILogicFactoryStore, LogicFactoryStore>();
                services.AddSingleton<IDriverFactoryStore, DriverFactoryStore>();

                services.AddSingleton<IDriverLoader, DriverLoader>();
                services.AddSingleton<ILogicLoader, LogicLoader>();

                services.AddSingleton<MqttService, MqttService>();
                services.AddSingleton<IRemoteHandler>(provider => provider.GetService<MqttService>());
                services.AddSingleton<IRemoteServerHandler>(provider => provider.GetService<MqttService>());
                services.AddSingleton<IRemoteSender, MqttPublishService>();
                

                services.AddSingleton<PluginHandler, PluginHandler>();
                services.AddSingleton<IPluginHandler>(a => a.GetService<PluginHandler>());
                services.AddSingleton<IPluginLoader>(a => a.GetService<PluginHandler>());

                services.AddSingleton<IRuleInstanceVisuNotify, RuleInstanceVisuNotifier>();

                services.AddSingleton<CoreServer, CoreServer>();
                services.AddSingleton<IHostedService>(provider => provider.GetService<CoreServer>());
                services.AddSingleton<ICoreServer>(provider => provider.GetService<CoreServer>());

                services.AddSingleton<INotifyDriver, NotifyDriverHandler>();
                services.AddSingleton<IRuleEngineDispatcher, RuleEngineDispatcher>();

                services.AddSingleton<IDataBroadcast, DataBroadcastService>();

                services.AddSingleton<DatabaseTrendingValueStore, DatabaseTrendingValueStore>();

            }


            var mqttServerOptions = new MqttServerOptions()
            {
                ConnectionValidator = new MqttServerConnectionValidatorDelegate(a => MqttService.ValidateConnection(a, configuration, SystemLogger.Instance))
            };

            mqttServerOptions.DefaultEndpointOptions.BoundInterNetworkAddress = IPAddress.Any;
            mqttServerOptions.DefaultEndpointOptions.BoundInterNetworkV6Address = IPAddress.None;
            mqttServerOptions.DefaultEndpointOptions.IsEnabled = true;
            mqttServerOptions.DefaultEndpointOptions.Port = 1883;
            mqttServerOptions.DefaultEndpointOptions.ConnectionBacklog = 1000;

            services.AddHostedMqttServer(mqttServerOptions);

            services.AddAutomaticaVisualization(configuration);
            services.AddInternals(configuration);
        }
    }
}
