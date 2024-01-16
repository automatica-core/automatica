using System;
using System.IO;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Knx.DriverFactory;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Knx.Core.Ets;
using Xunit;

namespace P3.Driver.Knx.Tests.ETS
{
    public class ETSProjectToNodeConverterTests : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        [Fact]
        public async Task TestConvert()
        {
            var path = GetFile("ETS5_ImportTest");

            var p = new EtsProjectParser().ParseEtsFile(path);
            var knxInterface = CreateNodeInstance(KnxIpDriverFactory.KnxGateway);

            var nodes = await EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, p, knxInterface);

            Assert.True(nodes.Count > 0);
        }


        private String GetFile(string fileName)
        {
            return Path.Combine("ETS", fileName + ".knxproj");
        }
    }
}
