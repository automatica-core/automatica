using System;
using Automatica.Core.Automation;
using Automatica.Core.Shading;
using System.Net;
using Automatica.Core.Base;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Base.Visu;
using Automatica.Core.Driver;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.License;
using Automatica.Core.Internals.Plugins;
using Automatica.Core.Internals.Recorder;
using Automatica.Core.Logging;
using Automatica.Core.Notification;
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
using MQTTnet.AspNetCore;
using MQTTnet.Certificates;
using MQTTnet.Server;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Automatica.Core.Runtime
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAutomaticaCoreService(this IServiceCollection services, IConfiguration configuration, bool isElectronActive)
        {
            // singletons must not use the scoped AutomaticaContext, they create (and dispose) a context per operation
            services.AddAutomaticaContextFactory();
            services.AddAutomaticaDrivers();
            services.AddDispatcher();

            services.AddSingleton<ILocalizationProvider, LocalizationProvider>();

            services.AddSingleton<IVisualisationFactory, VisuTempInit>();
            services.AddSingleton<ITelegramMonitor, TelegramMonitor>();
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

                if (ServerInfo.InDocker)
                {
                    services.AddSingleton<DockerUpdateHandler, DockerUpdateHandler>();
                    services.AddSingleton<IUpdateHandler>(a => a.GetRequiredService<DockerUpdateHandler>());
                    services.AddSingleton<IAutoUpdateHandler>(a => a.GetRequiredService<DockerUpdateHandler>());
                }
                else
                {
                    services.AddSingleton<NativeUpdateHandler, NativeUpdateHandler>();
                    services.AddSingleton<IUpdateHandler>(a => a.GetRequiredService<NativeUpdateHandler>());
                    services.AddSingleton<IAutoUpdateHandler>(a => a.GetRequiredService<NativeUpdateHandler>());
                }

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
                services.AddAutomations();
                services.AddShading();

                services.AddSingleton<IDataBroadcast, DataBroadcastService>();

                services.AddSingleton<DatabaseTrendingValueStore, DatabaseTrendingValueStore>();
            }

            var mqttServerOptions = BuildMqttServerOptions(configuration);

            services.AddSingleton<MqttNodeRegistry>();
            services.AddSingleton(_ => new MqttConnectThrottle());
            services.AddSingleton(provider => new MqttBrokerAuthenticator(
                new DatabaseMqttSatelliteStore(configuration),
                provider.GetRequiredService<MqttNodeRegistry>(),
                MqttPluginCredentials.CreateDefault(),
                provider.GetRequiredService<MqttConnectThrottle>(),
                !String.Equals(configuration["server:mqttLegacyPluginCredentials"], "false", StringComparison.OrdinalIgnoreCase),
                new CoreLogger(configuration, null)));

            // Registered before the hosted MQTT server so the connection validator and the topic ACL are attached before
            // it starts (MQTTnet 3 had it in the server options; without it MQTTnet 5 accepts every client).
            services.AddSingleton(provider => new MqttConnectionValidation(provider.GetRequiredService<MqttServer>(), mqttServerOptions,
                configuration, provider.GetRequiredService<MqttNodeRegistry>(), provider.GetRequiredService<MqttBrokerAuthenticator>()));
            services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<MqttConnectionValidation>());
            services.AddHostedMqttServer(mqttServerOptions);

            services.AddAutomaticaVisualization(configuration);
            services.AddInternals(configuration);
            services.AddNotificationManager();
        }

        /// <summary>
        /// Plaintext listener on <c>server:mqttPort</c> (1883) and TLS listener on <c>server:mqttTlsPort</c> (8883, off
        /// with <c>server:mqttTls=false</c>) with the per-install broker certificate. The bind address is decided at
        /// start by <see cref="MqttConnectionValidation"/> (<c>server:mqttBind</c>).
        /// </summary>
        internal static MqttServerOptions BuildMqttServerOptions(IConfiguration configuration)
        {
            var mqttServerPort = configuration["server:mqttPort"];
            var mqttTlsPort = configuration["server:mqttTlsPort"];
            var tlsEnabled = !String.Equals(configuration["server:mqttTls"], "false", StringComparison.OrdinalIgnoreCase);

            var builder = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
                .WithDefaultEndpointBoundIPV6Address(IPAddress.None)
                .WithDefaultEndpointPort(String.IsNullOrEmpty(mqttServerPort) ? 1883 : Convert.ToInt32(mqttServerPort))
                .WithConnectionBacklog(1000);

            if (tlsEnabled)
            {
                builder = builder
                    .WithEncryptedEndpoint()
                    .WithEncryptedEndpointBoundIPAddress(IPAddress.Loopback)
                    .WithEncryptedEndpointBoundIPV6Address(IPAddress.None)
                    .WithEncryptedEndpointPort(String.IsNullOrEmpty(mqttTlsPort) ? 8883 : Convert.ToInt32(mqttTlsPort))
                    .WithEncryptionSslProtocol(SslProtocols.Tls12 | SslProtocols.Tls13)
                    .WithEncryptionCertificate(new MqttBrokerCertificateProvider(configuration));
            }

            return builder.Build();
        }

        private sealed class MqttBrokerCertificateProvider : ICertificateProvider
        {
            private readonly IConfiguration _configuration;

            public MqttBrokerCertificateProvider(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            // created lazily when the TLS listener starts
            public X509Certificate2 GetCertificate() => MqttBrokerCertificate.Get(new CoreLogger(_configuration, null));
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
