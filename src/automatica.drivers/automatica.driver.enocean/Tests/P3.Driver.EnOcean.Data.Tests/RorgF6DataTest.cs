
using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory;
using P3.Driver.EnOcean.DriverFactory.Driver;
using Xunit;

namespace P3.Driver.EnOcean.Data.Tests
{
    public class RorgF6DataTest : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private RadioErp1Packet CreatePacket(string data)
        {
            var byteData = Automatica.Core.Driver.Utility.Utils.StringToByteArray(data);

            var packet = EnOceanPacket.Parse(byteData);

            var telegram = EnOceanTelegramFactory.FromPacket(packet) as RadioErp1Packet;
            return telegram;
        }

        private async Task<EnOceanDriver> CreateDriverForF6(string serial, params Guid[] dataFieldGuid)
        {
            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);

            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);

            var rorg = CreateNodeInstance(EnOceanRorgF6Data.RorgF6Guid);
            learned.InverseThis2ParentNodeInstanceNavigation.Add(rorg);

            var func = CreateNodeInstance(EnOceanRorgF6Data.FunctionF6_02Guid);
            rorg.InverseThis2ParentNodeInstanceNavigation.Add(func);

            var type = CreateNodeInstance(EnOceanRorgF6Data.TypeF6_02_01_1Guid);
            type.SetProperty("enocean-serialnumber", serial);
            func.InverseThis2ParentNodeInstanceNavigation.Add(type);

            foreach (var guid in dataFieldGuid)
            {
                var df = CreateNodeInstance(guid);
                type.InverseThis2ParentNodeInstanceNavigation.Add(df);
            }

            var driver = await CreateDriver<EnOceanDriver>(driverNode);

            return driver;
        }


        [Fact]
        public async Task TestRockerSwitch_R1_AO() 
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var telegram = CreatePacket("55000707017AF630FEF7D96E0001FFFFFFFF460039");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(1, values.First().Value.Value);
        }

        [Fact]
        public async Task TestRockerSwitch_R2_AO()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF630FEF7D96E0001FFFFFFFF460039");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(1, values.First().Value.Value);
        }

        [Fact]
        public async Task TestRockerSwitch_R2_TeachIn()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF630FEF7D96E0001FFFFFFFF460039");
            driver.TelegramReceived(telegram);

           Assert.True(RadioErp1Packet.IsTechIn(telegram));
        }

        [Fact]
        public async Task TestRockerSwitch_R1_AI() 
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var telegram = CreatePacket("55000707017AF610FEF7D96E0001FFFFFFFF4400ED");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(0, values.First().Value.Value);
        }

        [Fact]
        public async Task TestRockerSwitch_R2_AI() 
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF610FEF7D96E0001FFFFFFFF4400ED");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(0, values.First().Value.Value);
        }



        [Fact]
        public async Task TestRockerSwitch_R1_BO() 
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var telegram = CreatePacket("55000707017AF670FEF7D96E0001FFFFFFFF5B007C");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(3, values.First().Value.Value);
        }

        [Fact]
        public async Task TestRockerSwitch_R2_BO() 
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF670FEF7D96E0001FFFFFFFF5B007C");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(3, values.First().Value.Value);
        }

        [Fact]
        public async Task TestRockerSwitch_R1_BI() 
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var telegram = CreatePacket("55000707017AF650FEF7D96E0001FFFFFFFF43007D");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(2, values.First().Value.Value);
        }

        [Fact]
        public async Task TestRockerSwitch_R2_BI()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF650FEF7D96E0001FFFFFFFF43007D");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(2, values.First().Value.Value);
        }



        [Fact]
        public async Task TestWindowHandle_00_0()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("00211D5A",
                EnOceanRorgF6Data.DataFieldF6_10_00_1_WIN_Guid);

            var telegram = CreatePacket("55 00 07 07 01 7A F6 D0 00 21 1D 5A 22 03 FF FF FF FF 41 00 DF".Replace(" ", ""));
            driver.TelegramReceived(telegram);

            Assert.Equal("00211D5A", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(2, values.First().Value.Value);
        }


        [Fact]
        public async Task TestWindowHandle_00_1()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForF6("00211D5A",
                EnOceanRorgF6Data.DataFieldF6_10_00_1_WIN_Guid);

            var telegram = CreatePacket("55 00 07 07 01 7A F6 E0 00 21 1D 5A 22 02 FF FF FF FF 43 00 AB".Replace(" ", ""));
            driver.TelegramReceived(telegram);

            Assert.Equal("00211D5A", telegram.SenderIdString);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(0, values.First().Value.Value);
        }
    }
}
