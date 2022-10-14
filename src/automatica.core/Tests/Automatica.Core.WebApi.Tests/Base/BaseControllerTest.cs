using System;
using System.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime;
using Automatica.Core.Runtime.Core;
using Automatica.Core.Runtime.Core.Plugins;
using Automatica.Core.Runtime.Core.Plugins.Drivers;
using Automatica.Core.Runtime.Core.Plugins.Logics;
using Automatica.Core.Runtime.Database;
using Automatica.Core.Runtime.IO;
using Automatica.Push;
using Automatica.Push.Hubs;
using Automatica.Push.LearnMode;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MQTTnet.Server;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Base
{
    [TestCaseOrderer("Automatica.Core.WebApi.Tests.Base.PriorityOrderer", "PriorityOrderer")]
    public class BaseControllerTest<T> : IDisposable where T : BaseController
    {
        public static string DatabaseFileName { get;} = "AUTOMATICA_TEST_" + Guid.NewGuid().ToString().Replace("-", "");

        public static string DatabaseFilePath => $"{DatabaseFileName}-test";
        protected ServiceProvider ServiceProvider { get; }

        protected IConfiguration Configuration { get; }

        protected T Controller { get; private set; }
        public BaseControllerTest()
        {
            var tmpFolder = Path.Combine(Path.GetTempPath(), DatabaseFilePath, Guid.NewGuid().ToString());
            var dbName = Path.Combine(tmpFolder, $"{DatabaseFilePath}.db");
            if (File.Exists(dbName))
            {
                File.Delete(dbName);
            }

            if (Directory.Exists(tmpFolder))
            {
                Directory.Delete(tmpFolder, true);
            }

            Directory.CreateDirectory(tmpFolder);

            var mockConfSection = new Mock<IConfigurationSection>();
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "AutomaticaDatabaseType")]).Returns("sqlite");
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "AutomaticaDatabaseSqlite")]).Returns($"Data Source={tmpFolder}/{DatabaseFilePath}.db");

            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(a => a.GetSection(It.Is<string>(s => s == "ConnectionStrings"))).Returns(mockConfSection.Object);

            var config = mockConfiguration.Object;
            Configuration = config;

            var services = new ServiceCollection();

            services.AddSingleton(config);
            services.AddAutomaticaCoreService(config, false);
            services.AddDbContext<AutomaticaContext>();
            services.AddSingleton<T>();

            services.AddAutomaticaPushServices(config, false);

            var hubClients = new Mock<IHubClients>();
            var clientProxy = new Mock<IClientProxy>();

            hubClients.SetupGet(clients => clients.All).Returns(() => clientProxy.Object);
            hubClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(() => clientProxy.Object);

            var dataHubMoq = new Mock<IHubContext<DataHub>>();
            dataHubMoq.SetupGet(a => a.Clients).Returns(() => hubClients.Object);

            services.AddSingleton(dataHubMoq.Object);
            var telegramHubMoq = new Mock<IHubContext<TelegramHub>>();
            services.AddSingleton(telegramHubMoq.Object);

            services.AddSingleton<ILogger<NotifyDriverHandler>>(NullLogger<NotifyDriverHandler>.Instance);
            services.AddSingleton<ILogger<RuleEngineDispatcher>>(NullLogger<RuleEngineDispatcher>.Instance);
            services.AddSingleton<ILogger<LogicLoader>>(NullLogger<LogicLoader>.Instance);
            services.AddSingleton<ILogger<DriverLoader>>(NullLogger<DriverLoader>.Instance);
            services.AddSingleton<ILogger<PluginHandler>>(NullLogger<PluginHandler>.Instance);
            services.AddSingleton<ILogger<LearnMode>>(NullLogger<LearnMode>.Instance);
            services.AddSingleton<ILogger>(NullLogger.Instance);

            var mqttServerMock = new Mock<IMqttServer>();
            services.AddSingleton<IMqttServer>(mqttServerMock.Object);


            ServiceProvider = services.BuildServiceProvider();

            DatabaseInit.EnsureDatabaseCreated(ServiceProvider);

            Controller = ServiceProvider.GetRequiredService<T>();
        }

        public void Dispose()
        {
           
        }
    }
}
