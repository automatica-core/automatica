using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.VkingBms.Driver.Exception;
using P3.Driver.VkingBms.DriverFactory;
using Xunit;

namespace P3.Driver.VkingBms.Tests
{
    public class VkingDriverNodeTests : DriverFactoryTestBase<VkingBmsDriverFactory>
    {
        private static readonly Guid PortPropertyId = new Guid("e9d7d9cb-24bc-405e-8201-c734552a60df");

        [Fact]
        public void InvalidCrc_IsHandledAsDataReadException()
        {
            // the pack loop catches DataReadException per pack, a crc error must not abort the other packs
            Assert.IsAssignableFrom<DataReadException>(new InvalidCrcException("crc"));
        }

        [Fact]
        public async Task Start_WithPortThatCannotBeOpened_DoesNotThrow()
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            var port = node.PropertyInstance.Single(a => a.This2PropertyTemplate == PortPropertyId);
            port.Value = "VKING_PORT_DOES_NOT_EXIST";

            var driver = await CreateDriver(node);
            Assert.IsType<VkingDriverNode>(driver);

            Assert.True(await driver.Init());
            // the port does not exist, start must still succeed so the poll timer can retry to open the port
            Assert.True(await driver.Start());

            await Task.Delay(1500);

            Assert.True(await driver.Stop());
        }
    }
}
