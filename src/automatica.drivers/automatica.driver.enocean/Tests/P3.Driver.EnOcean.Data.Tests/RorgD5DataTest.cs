using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory;
using P3.Driver.EnOcean.DriverFactory.Driver;
using Xunit;


[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Driver.EnOcean.Data.Tests
{
    public class RorgD5DataTest : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private RadioErp1Packet CreatePacket(string data)
        {
            var byteData = Automatica.Core.Driver.Utility.Utils.StringToByteArray(data);

            var packet = EnOceanPacket.Parse(byteData);

            var telegram = EnOceanTelegramFactory.FromPacket(packet) as RadioErp1Packet;
            return telegram;
        }

        private async Task<EnOceanDriver> CreateDriverForD5(string serial, Guid dataFieldGuid)
        {
            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);

            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);

            var bs = CreateNodeInstance(EnOceanRorgD5Data.RorgD5Guid);
            learned.InverseThis2ParentNodeInstanceNavigation.Add(bs);

            var contact = CreateNodeInstance(EnOceanRorgD5Data.FunctionD5_00Guid);
            bs.InverseThis2ParentNodeInstanceNavigation.Add(contact);

            var singleInput = CreateNodeInstance(EnOceanRorgD5Data.TypeD5_00_01_1Guid);
            singleInput.SetProperty("enocean-serialnumber", serial);
            contact.InverseThis2ParentNodeInstanceNavigation.Add(singleInput);

            var df = CreateNodeInstance(dataFieldGuid);
            singleInput.InverseThis2ParentNodeInstanceNavigation.Add(df);

            var driver = await CreateDriver<EnOceanDriver>(driverNode);

            return driver;
        }

        [Fact]
        public async Task TestCoBit()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForD5("019DC2E8", EnOceanRorgD5Data.DataFieldD5_00_01_1_CO_Guid);

            var telegram = CreatePacket("55000707017AD509019DC2E80001FFFFFFFF5E0011");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value.Value);

        }
        [Fact]
        public async Task TestLrnBit_False()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForD5("019DC2E8", EnOceanRorgD5Data.DataFieldD5_00_01_1_LRN_Guid);

            var telegram = CreatePacket("55000707017AD509019DC2E80001FFFFFFFF5E0011");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.False((bool)values.First().Value.Value);

        }
        [Fact]
        public async Task TestLrnBit_True()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForD5("019DC2E8", EnOceanRorgD5Data.DataFieldD5_00_01_1_LRN_Guid);

            var telegram = CreatePacket("55000707017AD510019DC2E80001FFFFFFFF5E0011");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value.Value);

        }
    }
}
