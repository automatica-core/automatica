using System;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Common;
using Automatica.Core.UnitTests.Drivers;
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

        private EnOceanDriver CreateDriverForD5(string serial, Guid dataFieldGuid)
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

            var driver = CreateDriver<EnOceanDriver>(driverNode);

            return driver;
        }

        [Fact]
        public void TestCoBit()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForD5("019DC2E8", EnOceanRorgD5Data.DataFieldD5_00_01_1_CO_Guid);

            var telegram = CreatePacket("55000707017AD509019DC2E80001FFFFFFFF5E0011");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value);

        }
        [Fact]
        public void TestLrnBit_False()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForD5("019DC2E8", EnOceanRorgD5Data.DataFieldD5_00_01_1_LRN_Guid);

            var telegram = CreatePacket("55000707017AD509019DC2E80001FFFFFFFF5E0011");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.False((bool)values.First().Value);

        }
        [Fact]
        public void TestLrnBit_True()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForD5("019DC2E8", EnOceanRorgD5Data.DataFieldD5_00_01_1_LRN_Guid);

            var telegram = CreatePacket("55000707017AD510019DC2E80001FFFFFFFF5E0011");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value);

        }
    }
}
