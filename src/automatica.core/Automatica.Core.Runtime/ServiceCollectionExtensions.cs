using System;
using System.Net;
using Automatica.Core.Base;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Base.Visu;
using Automatica.Core.Driver;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.License;
using Automatica.Core.Internals.Plugins;
using Automatica.Core.Internals.Recorder;
using Automatica.Core.Logging;
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
using Automatica.Core.Runtime.Recorder;
using Automatica.Core.Runtime.Recorder.Abstraction;
using Automatica.Core.Runtime.RemoteConnect;
using Automatica.Core.Runtime.RemoteConnect.Frp;
using Automatica.Core.Visu;
using Automatica.Push;
using Automatica.Push.LearnMode;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore.Extensions;
using MQTTnet.Server;

namespace Automatica.Core.Runtime
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAutomaticaCoreService(this IServiceCollection services, IConfiguration configuration, bool isElectronActive)
        {
            services.AddAutomaticaDrivers();
            services.AddDispatcher();

            services.AddSingleton<ILocalizationProvider, LocalizationProvider>();

            services.AddSingleton<IVisualisationFactory, VisuTempInit>();
            services.AddSingleton<ITelegramMonitor, TelegramMonitor>();
            services.AddTransient<IServerCloudApi, CloudApi>();
            services.AddTransient<ICloudApi, CloudApi>();
            services.AddSingleton<ILicenseContext, LicenseContext>();
            services.AddSingleton<ILicenseContract>(provider => provider.GetRequiredService<ILicenseContext>());
            services.AddSingleton<ILearnMode, LearnMode>();
            services.AddAutomaticaPushServices(configuration, isElectronActive);

            services.AddSingleton<IPluginInstaller, PluginInstaller>();
            services.AddSingleton<IRecorderFactory, RecorderFactory>();
            services.AddSingleton<ITrendingContext, TrendingContext>();
            services.AddSingleton<IRecorderContext>(a => a.GetRequiredService<ITrendingContext>());

            if (!isElectronActive)
            {
                services.AddSingleton<IDriverNodesStoreInternal, DriverNodesStoreInternal>();
                services.AddSingleton<INodeInstanceStore, NodeInstanceStore>();
                services.AddSingleton<ILogicInstancesStore, LogicInstanceStore>();
                services.AddSingleton<INodeTemplateCache, NodeTemplateCache>();

                services.AddSingleton<LogicStore, LogicStore>();
                services.AddSingleton<ILogicStore>(a => a.GetRequiredService<LogicStore>());
                services.AddSingleton<ILogicDataHandler>(a => a.GetRequiredService<LogicStore>());

                services.AddSingleton<LoadedNodeInstancesStore, LoadedNodeInstancesStore>();
                services.AddSingleton<INodeInstanceStateHandler>(a => a.GetRequiredService<LoadedNodeInstancesStore>());
                services.AddSingleton<ILoadedNodeInstancesStore>(a => a.GetRequiredService<LoadedNodeInstancesStore>());

                services.AddSingleton<NativeUpdateHandler, NativeUpdateHandler>();
                services.AddSingleton<IUpdateHandler>(a => a.GetRequiredService<NativeUpdateHandler>());
                services.AddSingleton<IAutoUpdateHandler>(a => a.GetRequiredService<NativeUpdateHandler>());

                services.AddSingleton<ILoadedStore, LoadedStore>();
                services.AddSingleton<ILogicFactoryStore, LogicFactoryStore>();
                services.AddSingleton<IDriverFactoryStore, DriverFactoryStore>();


                services.AddSingleton<IDriverLoader, DriverLoader>();
                services.AddSingleton<ILogicLoader, LogicLoader>();

                services.AddSingleton<MqttService, MqttService>();
                services.AddSingleton<IRemoteHandler>(provider => provider.GetRequiredService<MqttService>());
                services.AddSingleton<IRemoteServerHandler>(provider => provider.GetRequiredService<MqttService>());
                services.AddSingleton<IRemoteSender, MqttPublishService>();
                

                services.AddSingleton<PluginHandler, PluginHandler>();
                services.AddSingleton<IPluginHandler>(a => a.GetRequiredService<PluginHandler>());
                services.AddSingleton<IPluginLoader>(a => a.GetRequiredService<PluginHandler>());

                services.AddSingleton<IRuleInstanceVisuNotify, RuleInstanceVisuNotifier>();

                services.AddSingleton<CoreServer, CoreServer>();
                services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<CoreServer>());
                services.AddSingleton<ICoreServer>(provider => provider.GetRequiredService<CoreServer>());

                services.AddSingleton<INotifyDriver, NotifyDriverHandler>();
                services.AddSingleton<ILogicEngineDispatcher, LogicEngineDispatcher>();

                services.AddSingleton<IDataBroadcast, DataBroadcastService>();

                services.AddSingleton<DatabaseTrendingValueStore, DatabaseTrendingValueStore>();

            }


            var mqttServerOptions = new MqttServerOptions()
            {
                ConnectionValidator = new MqttServerConnectionValidatorDelegate(a => MqttService.ValidateConnection(a, configuration, new CoreLogger(configuration, null)))
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

        public static void AddAutomaticaRemoteConnectWithFrp(this IServiceCollection services,
            Action<FrpcOptions> settings)
        {
            services.AddAutomaticaRemoteConnectServices();
            services.AddFrpServices(settings);
        }

        public static void AddAutomaticaRemoteConnectWithFrp(this IServiceCollection services,
            IConfiguration settings)
        {
            services.AddAutomaticaRemoteConnectServices();
            services.AddFrpServices(settings);
        }

        private static void AddAutomaticaRemoteConnectServices(this IServiceCollection services)
        {
            services.AddSingleton<IRemoteConnectService, RemoteConnectService>();

            services.AddTransient<Func<IDriverContext, ITunnelingProvider>>(provider =>
            {
                return driverContext => new RemoteConnectProvider(provider.GetRequiredService<IRemoteConnectService>(), driverContext);
            });
            
        }
    }
}
