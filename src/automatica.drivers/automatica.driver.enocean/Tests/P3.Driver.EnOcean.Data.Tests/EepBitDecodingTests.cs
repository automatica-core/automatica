using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory;
using P3.Driver.EnOcean.DriverFactory.Driver;
using P3.Driver.EnOcean.DriverFactory.Driver.Data;
using Xunit;

namespace P3.Driver.EnOcean.Data.Tests
{
    /// <summary>
    /// EEP data field decoding: big-endian bit strings (offset 0 = MSB of DB_n), range/scale mapping and VLD payloads.
    /// </summary>
    public class EepBitDecodingTests : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private static byte[] SetBits(int byteCount, params (int Offset, int Length, long Value)[] fields)
        {
            var data = new byte[byteCount];
            foreach (var (offset, length, value) in fields)
            {
                for (var i = 0; i < length; i++)
                {
                    var bit = (value >> (length - 1 - i)) & 1;
                    if (bit == 1)
                    {
                        var pos = offset + i;
                        data[pos >> 3] |= (byte)(0x80 >> (pos & 7));
                    }
                }
            }
            return data;
        }

        private static byte Crc8(IEnumerable<byte> data)
        {
            byte crc = 0;
            foreach (var b in data)
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ 0x07) : (byte)(crc << 1);
                }
            }
            return crc;
        }

        private static RadioErp1Packet Radio(Rorg rorg, byte[] userData, string sender)
        {
            var erp1 = new List<byte> { (byte)rorg };
            erp1.AddRange(userData);
            erp1.AddRange(Automatica.Core.Driver.Utility.Utils.StringToByteArray(sender));
            erp1.Add(0x00);
            var opt = new byte[] { 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x46, 0x00 };
            var header = new byte[] { (byte)(erp1.Count >> 8), (byte)erp1.Count, (byte)opt.Length, 0x01 };

            var frame = new List<byte> { 0x55 };
            frame.AddRange(header);
            frame.Add(Crc8(header));
            frame.AddRange(erp1);
            frame.AddRange(opt);
            frame.Add(Crc8(erp1.Concat(opt)));

            return (RadioErp1Packet)EnOceanTelegramFactory.FromPacket(EnOceanPacket.Parse(frame.ToArray()));
        }

        [Theory]
        [InlineData(0, 8, 0xA5L)]
        [InlineData(8, 8, 0x0FL)]
        [InlineData(0, 1, 1L)]
        [InlineData(1, 1, 0L)]
        [InlineData(4, 8, 0x50L)]   // crosses the byte boundary: low nibble of DB3 + high nibble of DB2
        [InlineData(6, 10, 0x10FL)] // 10 bit field across two bytes
        [InlineData(0, 32, 0xA50FF001L)]
        [InlineData(28, 4, 0x1L)]
        public void ExtractBits_ReadsBigEndianBitStrings(int offset, int length, long expected)
        {
            var data = new byte[] { 0xA5, 0x0F, 0xF0, 0x01 };

            Assert.Equal(expected, EnOceanDataNode.ExtractBits(data, offset, length));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 33)]
        [InlineData(-1, 4)]
        [InlineData(30, 4)]
        [InlineData(32, 1)]
        public void ExtractBits_FieldOutsideTheData_IsNull(int offset, int length)
        {
            Assert.Null(EnOceanDataNode.ExtractBits(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, offset, length));
        }

        [Fact]
        public void ExtractBits_RoundTripsEveryOffsetAndLength()
        {
            var random = new Random(1234);
            for (var length = 1; length <= 32; length++)
            {
                for (var offset = 0; offset + length <= 48; offset += 3)
                {
                    var value = (long)(random.NextInt64() & ((1L << length) - 1));
                    var data = SetBits(6, (offset, length, value));

                    Assert.Equal(value, EnOceanDataNode.ExtractBits(data, offset, length));
                }
            }
        }

        private EnOceanGenericData Node() =>
            new(new DriverContextMock(CreateNodeInstance(EnOceanDriverFactory.DriverGuidId), DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System), null);

        [Fact]
        public void ScaleMinMax_WithoutScale_KeepsTheRawIntValue()
        {
            var node = Node();

            Assert.Equal(42, node.ScaleMinMax(42));
            Assert.IsType<int>(node.ScaleMinMax(42));
        }

        [Fact]
        public void ScaleMinMax_ValueAboveIntRange_StaysLong()
        {
            var node = Node();

            Assert.Equal(0xFFFFFFFFL, node.ScaleMinMax(0xFFFFFFFFL));
        }

        [Theory]
        [InlineData(0, -40.0)]
        [InlineData(255, 0.0)]
        [InlineData(127, -20.0784313725)]
        public void ScaleMinMax_MapsTheRangeLinearly(long raw, double expected)
        {
            // A5-02-05: 255..0 => -40..0 °C (inverted range)
            var node = Node();
            node.UseRange = true;
            node.UseScale = true;
            node.RangeMin = 255;
            node.RangeMax = 0;
            node.ScaleMin = 0;
            node.ScaleMax = -40;
            node.Multiplier = (node.ScaleMax - node.ScaleMin) / (node.RangeMax - node.RangeMin);

            var value = Assert.IsType<double>(node.ScaleMinMax(raw));
            Assert.Equal(expected, value, 6);
        }

        [Fact]
        public async Task Vld_D2_14_30_DecodesFieldsBeyondTheFirstFourBytes()
        {
            // regression: VLD telegrams had a data length of 0, nothing was decoded
            await Dispatcher.ClearValues();

            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);
            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);
            var rorg = CreateNodeInstance(EnOceanRorgD2Data.RorgD2Guid);
            learned.InverseThis2ParentNodeInstanceNavigation.Add(rorg);
            var func = CreateNodeInstance(EnOceanRorgD2Data.FunctionD2_14Guid);
            rorg.InverseThis2ParentNodeInstanceNavigation.Add(func);
            var type = CreateNodeInstance(EnOceanRorgD2Data.TypeD2_14_30_27Guid);
            type.SetProperty("enocean-serialnumber", "05123456");
            func.InverseThis2ParentNodeInstanceNavigation.Add(type);

            var tmp = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_Guid);
            var hum = CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_HUM_Guid);
            type.InverseThis2ParentNodeInstanceNavigation.Add(tmp);
            type.InverseThis2ParentNodeInstanceNavigation.Add(hum);

            var driver = await CreateDriver<EnOceanDriver>(driverNode);

            // TMP8: offset 23, 8 bit; HUM: offset 31, 8 bit (ends in the 5th data byte)
            var payload = SetBits(6, (23, 8, 0x5A), (31, 8, 0xC3));
            driver.TelegramReceived(Radio(Rorg.Vld, payload, "05123456"));

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);
            // TMP8 0..250 => 0..50 °C, HUM 0..200 => 0..100 % (EEP enum segments)
            Assert.Equal(0x5A * 50.0 / 250, (double)values[tmp.ObjId].Value, 6);
            Assert.Equal(0xC3 * 100.0 / 200, (double)values[hum.ObjId].Value, 6);
        }

        [Fact]
        public async Task Vld_OtherSender_IsIgnored()
        {
            await Dispatcher.ClearValues();

            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);
            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);
            var rorg = CreateNodeInstance(EnOceanRorgD2Data.RorgD2Guid);
            learned.InverseThis2ParentNodeInstanceNavigation.Add(rorg);
            var func = CreateNodeInstance(EnOceanRorgD2Data.FunctionD2_14Guid);
            rorg.InverseThis2ParentNodeInstanceNavigation.Add(func);
            var type = CreateNodeInstance(EnOceanRorgD2Data.TypeD2_14_30_27Guid);
            type.SetProperty("enocean-serialnumber", "05123456");
            func.InverseThis2ParentNodeInstanceNavigation.Add(type);
            type.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_Guid));

            var driver = await CreateDriver<EnOceanDriver>(driverNode);
            driver.TelegramReceived(Radio(Rorg.Vld, SetBits(6, (23, 8, 0x5A)), "05FFFFFF"));

            Assert.Empty(Dispatcher.GetValues(DispatchableType.NodeInstance));
        }
    }
}
