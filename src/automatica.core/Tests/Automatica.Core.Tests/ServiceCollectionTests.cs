using System.Linq;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Visu;
using Automatica.Core.Driver;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Core.HyperSeries;
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
using Automatica.Core.Runtime.IO;
using Automatica.Core.Visu;
using Automatica.Push.Hubs;
using Automatica.Push.LearnMode;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Automatica.Core.Tests
{

    public class ServiceCollectionTests
    {
     
        private ServiceCollection Initialize()
        {
            var mockConfSection = new Mock<IConfigurationSection>();
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "AutomaticaDatabaseType")]).Returns("sqlite");
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "AutomaticaDatabaseSqlite")]).Returns("Data Source=automatica.core-test.db");

            var mockConfiguration = new Mock<IConfigurationRoot>();
            mockConfiguration.Setup(a => a.GetSection(It.Is<string>(s => s == "ConnectionStrings"))).Returns(mockConfSection.Object);
            var moq = new ServiceCollection();

            Runtime.ServiceCollectionExtensions.AddAutomaticaCoreService(moq, mockConfSection.Object, false);
            Runtime.ServiceCollectionExtensions.AddAutomaticaRemoteConnectWithFrp(moq, mockConfiguration.Object);
            Internals.ServiceCollectionExtension.AddInternals(moq, mockConfiguration.Object);


            moq.AddSingleton(CreateHubContextMock<TelegramHub>());
            moq.AddSingleton(CreateHubContextMock<DataHub>());
            moq.AddSingleton(new AutomaticaContext(mockConfiguration.Object));
            moq.AddSingleton(new HyperSeriesContext(mockConfiguration.Object));
            moq.AddSingleton<ILogger>(a => NullLogger.Instance);
            moq.AddSingleton<IHyperSeriesRepository, HyperSeriesRepository>();

            moq.AddLogging(a => { a.AddConsole(); });

            moq.AddSingleton<IConfigurationRoot>(mockConfiguration.Object);
            moq.AddSingleton<IConfiguration>(mockConfiguration.Object);
            return moq;
        }

        private IHubContext<T> CreateHubContextMock<T>() where T : Hub
        {
            var mock = new Mock<IHubContext<T>>();
            var hubClients = new Mock<IHubClients>();

            hubClients.SetupGet(a => a.All).Returns(new Mock<IClientProxy>().Object);

            mock.SetupGet(a => a.Clients).Returns(hubClients.Object);

            return mock.Object;
        }

        [Fact]
        public void CoreServerAssertLifetime()
        {
            var moq = Initialize();

            AssertLifecycle<ILocalizationProvider>(moq, ServiceLifetime.Transient);
            AssertLifecycle<IVisualisationFactory>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ITelegramMonitor>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IServerCloudApi>(moq, ServiceLifetime.Transient);
            AssertLifecycle<ICloudApi>(moq, ServiceLifetime.Transient);
            AssertLifecycle<ILicenseContext>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ILicenseContract>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ILearnMode>(moq, ServiceLifetime.Singleton);

            AssertLifecycle<INodeInstanceStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IDriverStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IDriverNodesStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ILogicInstancesStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<LogicStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ILogicStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ILogicDataHandler>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<LoadedNodeInstancesStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<INodeInstanceStateHandler>(moq, ServiceLifetime.Singleton);

            AssertLifecycle<ILoadedNodeInstancesStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IUpdateHandler>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IAutoUpdateHandler>(moq, ServiceLifetime.Singleton);

            if (ServerInfo.InDocker)
            {
                AssertLifecycle<DockerUpdateHandler>(moq, ServiceLifetime.Singleton);
            }
            else
            {
                AssertLifecycle<NativeUpdateHandler>(moq, ServiceLifetime.Singleton);
            }

            AssertLifecycle<ILoadedStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ILogicFactoryStore>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IDriverFactoryStore>(moq, ServiceLifetime.Singleton);

            AssertLifecycle<IDriverLoader>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ILogicLoader>(moq, ServiceLifetime.Singleton);


            AssertLifecycle<MqttService>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IRemoteHandler>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IRemoteServerHandler>(moq, ServiceLifetime.Singleton);
            
            AssertLifecycle<PluginHandler>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IPluginHandler>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IPluginLoader>(moq, ServiceLifetime.Singleton);

            AssertLifecycle<IRuleInstanceVisuNotify>(moq, ServiceLifetime.Singleton);

            AssertLifecycle<CoreServer>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ICoreServer>(moq, ServiceLifetime.Singleton);

            AssertLifecycle<INotifyDriver>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<IDispatcher>(moq, ServiceLifetime.Singleton);
            AssertLifecycle<ILogicEngineDispatcher>(moq, ServiceLifetime.Singleton);
            
        }


        [Fact]
        public void CoreServerAssertImplementation()
        {
            var moq = Initialize();


            AssertImplementationType<ILocalizationProvider, LocalizationProvider>(moq);
            AssertImplementationType<IVisualisationFactory, VisuTempInit>(moq);

            AssertImplementationType<ITelegramMonitor, TelegramMonitor>(moq);
            AssertImplementationType<IServerCloudApi, ServerCloudApi>(moq);
            AssertImplementationType<ICloudApi, CloudApi>(moq);
            AssertImplementationType<ILicenseContext, LicenseContext>(moq);
            AssertImplementationType<ILicenseContract, LicenseContext>(moq);
            AssertImplementationType<ILearnMode, LearnMode>(moq);

            AssertImplementationType<INodeInstanceStore, NodeInstanceStore>(moq);
            AssertImplementationType<IDriverStore, DriverStore>(moq);

            AssertImplementationType<IDriverNodesStore, DriverNodesStore>(moq);
            AssertImplementationType<ILogicInstancesStore, LogicInstanceStore>(moq);

            AssertImplementationType<LogicStore, LogicStore>(moq);
            AssertImplementationType<ILogicStore, LogicStore>(moq);
            AssertImplementationType<ILogicDataHandler, LogicStore>(moq);

            AssertImplementationType<LoadedNodeInstancesStore, LoadedNodeInstancesStore>(moq);
            AssertImplementationType<INodeInstanceStateHandler, LoadedNodeInstancesStore>(moq);
            AssertImplementationType<ILoadedNodeInstancesStore, LoadedNodeInstancesStore>(moq);

            AssertImplementationType<ILoadedStore, LoadedStore>(moq);
            AssertImplementationType<ILogicFactoryStore, LogicFactoryStore>(moq);
            AssertImplementationType<IDriverFactoryStore, DriverFactoryStore>(moq);

            AssertImplementationType<IDriverLoader, DriverLoader>(moq);
            AssertImplementationType<ILogicLoader, LogicLoader>(moq);


            AssertImplementationType<MqttService, MqttService>(moq);
            AssertImplementationType<IRemoteHandler, MqttService>(moq);
            AssertImplementationType<IRemoteServerHandler, MqttService>(moq);

            AssertImplementationType<PluginHandler, PluginHandler>(moq);
            AssertImplementationType<IPluginHandler, PluginHandler>(moq);
            AssertImplementationType<IPluginLoader, PluginHandler>(moq);

            AssertImplementationType<IRuleInstanceVisuNotify, RuleInstanceVisuNotifier>(moq);

            AssertImplementationType<CoreServer, CoreServer>(moq);
            AssertImplementationType<ICoreServer, CoreServer>(moq);

            AssertImplementationType<INotifyDriver, NotifyDriverHandler>(moq);
            AssertImplementationType<IDispatcher, Base.IO.Dispatcher>(moq);
            AssertImplementationType<ILogicEngineDispatcher, LogicEngineDispatcher>(moq);

            if (ServerInfo.InDocker)
            {
                AssertImplementationType<IAutoUpdateHandler, DockerUpdateHandler>(moq);
            }
            else
            {
                AssertImplementationType<IAutoUpdateHandler, NativeUpdateHandler>(moq);
            }
        }


        private void AssertLifecycle<T>(IServiceCollection moq, ServiceLifetime lifeTime)
        {
            var item = moq.FirstOrDefault(t => t.ServiceType == typeof(T));

            Assert.NotNull(item);

            Assert.True(item.Lifetime == lifeTime);
        }

        private void AssertImplementationType<T, T2>(IServiceCollection moq)
        {
            var serviceProvider = moq.BuildServiceProvider(false);
            var service = serviceProvider.GetService(typeof(T));

            Assert.NotNull(service);

            Assert.IsType<T2>(service);
        }
    }
}
