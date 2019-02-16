
using System;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Common;
using Automatica.Core.UnitTests.Drivers;
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

        private EnOceanDriver CreateDriverForF6(string serial, params Guid[] dataFieldGuid)
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

            var driver = CreateDriver<EnOceanDriver>(driverNode);

            return driver;
        }


        [Fact]
        public void TestRockerSwitch_R1_AO() 
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var telegram = CreatePacket("55000707017AF630FEF7D96E0001FFFFFFFF460039");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(1, values.First().Value);
        }

        [Fact]
        public void TestRockerSwitch_R2_AO()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF630FEF7D96E0001FFFFFFFF460039");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(1, values.First().Value);
        }

        [Fact]
        public void TestRockerSwitch_R2_TeachIn()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF630FEF7D96E0001FFFFFFFF460039");
            driver.TelegramReceived(telegram);

           Assert.True(RadioErp1Packet.IsTechIn(telegram));
        }

        [Fact]
        public void TestRockerSwitch_R1_AI() 
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var telegram = CreatePacket("55000707017AF610FEF7D96E0001FFFFFFFF4400ED");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(0, values.First().Value);
        }

        [Fact]
        public void TestRockerSwitch_R2_AI() 
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF610FEF7D96E0001FFFFFFFF4400ED");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(0, values.First().Value);
        }



        [Fact]
        public void TestRockerSwitch_R1_BO() 
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var telegram = CreatePacket("55000707017AF670FEF7D96E0001FFFFFFFF5B007C");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(3, values.First().Value);
        }

        [Fact]
        public void TestRockerSwitch_R2_BO() 
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF670FEF7D96E0001FFFFFFFF5B007C");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(3, values.First().Value);
        }

        [Fact]
        public void TestRockerSwitch_R1_BI() 
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid);

            var telegram = CreatePacket("55000707017AF650FEF7D96E0001FFFFFFFF43007D");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(2, values.First().Value);
        }

        [Fact]
        public void TestRockerSwitch_R2_BI() 
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForF6("FEF7D96E",
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R2_Guid);

            var telegram = CreatePacket("55000707017AF650FEF7D96E0001FFFFFFFF43007D");
            driver.TelegramReceived(telegram);

            Assert.Equal("FEF7D96E", telegram.SenderIdString);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            Assert.Equal(2, values.First().Value);
        }
    }
}
