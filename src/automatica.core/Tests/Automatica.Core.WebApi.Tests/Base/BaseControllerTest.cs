using System;
using System.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime;
using Automatica.Core.Runtime.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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

        protected T Controller { get; }
        public BaseControllerTest()
        {
            var tmpFolder = Path.Combine(Path.GetTempPath(), DatabaseFilePath);
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
               

            ServiceProvider = services.BuildServiceProvider();

            DatabaseInit.EnsureDatabaseCreated(ServiceProvider);

            Controller = ServiceProvider.GetRequiredService<T>();
        }

        public void Dispose()
        {
            var tmpFolder = Path.Combine(Path.GetTempPath(), DatabaseFilePath);
            var dbName = Path.Combine(tmpFolder, $"{DatabaseFilePath}.db");
            if (File.Exists(dbName))
            {
                File.Delete(dbName);
            }

            if (Directory.Exists(tmpFolder))
            {
                Directory.Delete(tmpFolder, true);
            }
        }
    }
}
