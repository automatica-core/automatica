using System;
using System.IO;
using Automatica.Core.Base.Visu;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Database;
using Automatica.Core.Visu;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.Database
{
    public class DatabaseInitTest
    {
        [Fact]
        public void TestDatabaseInit()
        {
            if (File.Exists("automatica.core-test.db"))
            {
                File.Delete("automatica.core-test.db");
            }

            var mockConfSection = new Mock<IConfigurationSection>();
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "AutomaticaDatabaseType")]).Returns("sqlite");
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "AutomaticaDatabaseSqlite")]).Returns("Data Source=automatica.core-test.db");

            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(a => a.GetSection(It.Is<string>(s => s == "ConnectionStrings"))).Returns(mockConfSection.Object);


            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(a => a.GetService(It.Is<Type>(s => s == typeof(IConfiguration)))).Returns(mockConfiguration.Object);
            serviceProviderMock.Setup(a => a.GetService(It.Is<Type>(s => s == typeof(AutomaticaContext)))).Returns(new AutomaticaContext(mockConfiguration.Object, true));
            serviceProviderMock.Setup(a => a.GetService(It.Is<Type>(s => s == typeof(IVisualisationFactory)))).Returns(new VisuTempInit());

            try
            {
                DatabaseInit.EnsureDatabaseCreated(serviceProviderMock.Object);
                Assert.True(true);
            }
            catch (Exception e)
            {
                Assert.True(false);
                Console.Error.WriteLine($"{e}");
            }
           
        }
    }
}
