using Automatica.Core.UnitTests.Drivers;
using P3.Driver.Knx.DriverFactory;
using P3.Knx.Core.Ets;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace P3.Driver.Knx.Tests.Ets
{
    public class ETSProjectToNodeConverterTests : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        [Fact]
        public async Task TestConvert()
        {
            var path = GetFile("ETS5_ImportTest");

            var p = new EtsProjectParser().ParseEtsFile(path);
            var knxInterface = CreateNodeInstance(KnxIpDriverFactory.KnxGatway);

            var nodes = await EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, p, knxInterface);

            Assert.True(nodes.Count > 0);
        }


        private String GetFile(string fileName)
        {
            return Path.Combine("ETS", fileName + ".knxproj");
        }
    }
}
