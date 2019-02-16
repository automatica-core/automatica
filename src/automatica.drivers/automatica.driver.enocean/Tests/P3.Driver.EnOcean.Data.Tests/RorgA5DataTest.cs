﻿    
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
    public class RorgA5DataTest : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private RadioErp1Packet CreatePacket(string data)
        {
            var byteData = Automatica.Core.Driver.Utility.Utils.StringToByteArray(data);

            var packet = EnOceanPacket.Parse(byteData);

            var telegram = EnOceanTelegramFactory.FromPacket(packet) as RadioErp1Packet;
            return telegram;
        }

        private EnOceanDriver CreateDriverForA5(string serial, params Guid[] dataFieldGuid)
        {
            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);

            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);

            var rorg = CreateNodeInstance(EnOceanRorgA5Data.RorgA5Guid);
            learned.InverseThis2ParentNodeInstanceNavigation.Add(rorg);

            var func = CreateNodeInstance(EnOceanRorgA5Data.FunctionA5_11Guid);
            rorg.InverseThis2ParentNodeInstanceNavigation.Add(func);

            var type = CreateNodeInstance(EnOceanRorgA5Data.TypeA5_11_01_1Guid);
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
        public void TestA5_11_01_DataP1()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_P1_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0, values.First().Value);
        }

        [Fact]
        public void TestA5_11_01_Data_TeachIn()
        {
            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            
            Assert.False(RadioErp1Packet.IsTechIn(telegram));
        }

        [Fact]
        public void TestA5_11_01_Data_TeachIn2()
        {
            var telegram = CreatePacket("55000707017AF6D000211D5A2001FFFFFFFF500002");
            
            Assert.True(RadioErp1Packet.IsTechIn(telegram));
        }

        [Fact]
        public void TestA5_11_01_DataP2()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_P2_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0, values.First().Value);
        }
        [Fact]
        public void TestA5_11_01_DataP3()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_P3_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(142, values.First().Value);
        }

        [Fact]
        public void TestA5_11_01_DataPM()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_PM_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0, values.First().Value);
        }
        [Fact]
        public void TestA5_11_01_DataLRN()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_LRNB_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.False((bool)values.First().Value);
        }
        [Fact]
        public void TestA5_11_01_DataES()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_ES_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0, values.First().Value);
        }

        [Fact]
        public void TestA5_11_01_DataOHF()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_OHF_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value);
        }


        [Fact]
        public void TestA5_02_05_TMP()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000055080181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(26.7, Math.Round((double)values.First().Value, 1));
        }
        [Fact]
        public void TestA5_02_05_TMP2()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0185E268", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000077080185E2680001FFFFFFFF44004C");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(21.3, Math.Round((double)values.First().Value, 1));
        }
        [Fact]
        public void TestA5_02_05_TMP_Min()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5FFFFFFFF0181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0.0, Math.Round((double)values.First().Value, 1));
        }
        [Fact]
        public void TestA5_02_05_TMP_Max()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000000000181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(40.0, Math.Round((double)values.First().Value, 1));
        }

        [Fact]
        public void TestA5_02_20_TMP()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000055080181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(28.4, Math.Round((double)values.First().Value, 1));
        }

        [Fact]
        public void TestA5_02_20_TMP_Min()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5FFFFFFFF0181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(-10, Math.Round((double)values.First().Value, 1));
        }

        [Fact]
        public void TestA5_02_20_TMP_Max()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000000000181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(41.2, Math.Round((double)values.First().Value, 1));
        }



        [Fact]
        public void TestA5_02_03_TMP()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000055080181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(6.7, Math.Round((double)values.First().Value, 1));
        }

        [Fact]
        public void TestA5_02_03_TMP_Min()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5FFFFFFFF0181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(-20.0, Math.Round((double)values.First().Value, 1));
        }

        [Fact]
        public void TestA5_02_03_TMP_Max()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000000000181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(20.0, Math.Round((double)values.First().Value, 1));
        }
    }
}
