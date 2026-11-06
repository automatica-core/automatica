    
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
    public class RorgA5DataTest : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private RadioErp1Packet CreatePacket(string data)
        {
            var byteData = Automatica.Core.Driver.Utility.Utils.StringToByteArray(data);

            var packet = EnOceanPacket.Parse(byteData);

            var telegram = EnOceanTelegramFactory.FromPacket(packet) as RadioErp1Packet;
            return telegram;
        }

        private async Task<EnOceanDriver> CreateDriverForA5(string serial, params Guid[] dataFieldGuid)
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

            var driver = await CreateDriver<EnOceanDriver>(driverNode);

            return driver;
        }

        [Fact]
        public async Task TestA5_11_01_DataP1()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_P1_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0, values.First().Value.Value);
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
        public void TestA5_TeachIn_LrnBitCleared()
        {
            // A5-02-05 teach-in: DB0 = 0x80 (LRN type = 1, LRN bit DB0.3 = 0)
            var telegram = CreatePacket("55000A0701EBA5082846800181B7440001FFFFFFFF460069");

            Assert.True(RadioErp1Packet.IsTechIn(telegram));
            Assert.True(RadioErp1Packet.IsLrnTeachIn(telegram));
        }

        [Fact]
        public void TestA5_Data_LrnBitSet()
        {
            // DB0 = 0x0E: LRN bit set -> data telegram (the old 0x16 mask flagged this as teach-in)
            var telegram = CreatePacket("55000A0701EBA50000000E0181B7440001FFFFFFFF46008B");

            Assert.False(RadioErp1Packet.IsTechIn(telegram));
            Assert.False(RadioErp1Packet.IsLrnTeachIn(telegram));
        }

        [Fact]
        public async Task TestA5_11_01_DataP2()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_P2_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0, values.First().Value.Value);
        }

        [Fact]
        public async Task TestA5_11_01_DataP3()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_P3_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(142, values.First().Value.Value);
        }

        [Fact]
        public async Task TestA5_11_01_DataPM()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_PM_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0, values.First().Value.Value);
        }

        [Fact]
        public async Task TestA5_11_01_DataLRN()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_LRNB_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.False((bool)values.First().Value.Value);
        }

        [Fact]
        public async Task TestA5_11_01_DataES()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_ES_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0, values.First().Value.Value);
        }

        [Fact]
        public async Task TestA5_11_01_DataOHF()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("FFA8AB00",
                EnOceanRorgA5Data.DataFieldA5_11_04_4_OHF_Guid);

            var telegram = CreatePacket("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value.Value);
        }


        [Fact]
        public async Task TestA5_02_05_TMP()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000055080181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(26.7, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_05_TMP2()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0185E268", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000077080185E2680001FFFFFFFF44004C");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(21.3, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_05_TMP_Min()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5FFFFFFFF0181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(0.0, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_05_TMP_Max()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000000080181B7440001FFFFFFFF4600FA");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(40.0, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_20_TMP()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000055080181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(36.9, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_20_TMP_Min()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5FFFFFFFF0181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(-10, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_20_TMP_Max()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_20_24_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000000080181B7440001FFFFFFFF4600FA");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(41.2, Math.Round((double)values.First().Value.Value, 1));
        }



        [Fact]
        public async Task TestA5_02_03_TMP()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000055080181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(6.7, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_03_TMP_Min()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5FFFFFFFF0181B7440001FFFFFFFF2D0075");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(-20.0, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_03_TMP_Max()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_03_3_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5000000080181B7440001FFFFFFFF4600FA");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(20.0, Math.Round((double)values.First().Value.Value, 1));
        }



        [Fact]
        public async Task TestA5_04_03_TMP()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("05194725", EnOceanRorgA5Data.DataFieldA5_04_03_3_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5CF025209051947250101FFFFFFFF5C00C0");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(26.5d, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_04_03_HUM()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("05194725", EnOceanRorgA5Data.DataFieldA5_04_03_3_HUM_Guid);

            var telegram = CreatePacket("55000A0701EBA5CF025209051947250101FFFFFFFF5C00C0");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.NotNull(values);

            Assert.Equal(1, values.Count);
            Assert.Equal(81.2d, Math.Round((double)values.First().Value.Value, 1));
        }

        [Fact]
        public async Task TestA5_02_05_TeachIn_NotDispatched()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var telegram = CreatePacket("55000A0701EBA5082846800181B7440001FFFFFFFF460069");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Empty(values);
        }

        [Fact]
        public async Task TestA5_02_05_TeachIn_LrnNodeStillDispatched()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_02_05_5_LRNB_Guid);

            var telegram = CreatePacket("55000A0701EBA5082846800181B7440001FFFFFFFF460069");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value.Value);
        }

        [Fact]
        public async Task TestA5_07_03_ILL_10Bit()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_07_03_3_ILL_Guid);

            // ILL offset 8 length 10: DB2 = 0xA9, DB1 = 0x40 -> 10 1010 0101 = 677 lx
            var telegram = CreatePacket("55000A0701EBA500A940080181B7440001FFFFFFFF4600D0");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(677d, Math.Round(Convert.ToDouble(values.First().Value.Value), 1));
        }

        [Fact]
        public async Task TestA5_07_01_PIRS_On()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_07_01_1_PIRS_Guid);

            // PIRS 200 is in the 128..255 "motion detected" range
            var telegram = CreatePacket("55000A0701EBA50000C8080181B7440001FFFFFFFF4600F1");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value.Value);
        }

        [Fact]
        public async Task TestA5_12_01_MR_24Bit()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_12_01_2_MR_Guid);

            // MR = DB3..DB1 = 0x012345, DB0 = 0x0E (TI 0, LRN 1, DT 1, DIV 2)
            var telegram = CreatePacket("55000A0701EBA50123450E0181B7440001FFFFFFFF460004");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(0x012345, Convert.ToInt32(values.First().Value.Value));
        }

        [Fact]
        public async Task TestA5_12_01_DIV()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_12_01_2_DIV_Guid);

            var telegram = CreatePacket("55000A0701EBA50123450E0181B7440001FFFFFFFF460004");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(2, values.First().Value.Value);
        }

        [Fact]
        public async Task TestA5_12_01_DT()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriverForA5("0181B744", EnOceanRorgA5Data.DataFieldA5_12_01_2_DT_Guid);

            var telegram = CreatePacket("55000A0701EBA50123450E0181B7440001FFFFFFFF460004");
            driver.TelegramReceived(telegram);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);
            Assert.True((bool)values.First().Value.Value);
        }
    }
}
