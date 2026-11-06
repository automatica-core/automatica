using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Frames.VariableData;
using Xunit;

namespace P3.Driver.MBus.Tests
{
    public class DataDecodingTests
    {
        private static List<VariableDataBlock> Parse(string hex)
        {
            var frame = new VariableDataFrame();
            frame.ParseVariableDataFrame(Utils.StringToByteArray(hex), 0, NullLogger.Instance);
            return frame.DataBlocks;
        }

        private static byte[] BuildLongFrame(byte ci, byte[] userData)
        {
            var frame = new byte[9 + userData.Length];
            frame[0] = 0x68;
            frame[1] = (byte)(3 + userData.Length);
            frame[2] = (byte)(3 + userData.Length);
            frame[3] = 0x68;
            frame[4] = 0x08;
            frame[5] = 0x01;
            frame[6] = ci;
            Array.Copy(userData, 0, frame, 7, userData.Length);

            byte cs = 0;
            for (int i = 4; i < 7 + userData.Length; i++)
            {
                cs += frame[i];
            }
            frame[7 + userData.Length] = cs;
            frame[8 + userData.Length] = 0x16;
            return frame;
        }

        [Fact]
        public void Bcd8Digits()
        {
            var blocks = Parse("0C0378563412");
            Assert.Equal(12345678d, blocks[0].Value);
        }

        [Fact]
        public void Bcd2Digits()
        {
            var blocks = Parse("090325");
            Assert.Equal(25d, blocks[0].Value);
        }

        [Fact]
        public void BcdNegative()
        {
            // 0xF in the most significant nibble = negative
            var blocks = Parse("0A0334F2");
            Assert.Equal(-234d, blocks[0].Value);
        }

        [Fact]
        public void Bcd12DigitsWithMultiplier()
        {
            // VIF 0x13 = volume 10^-3 m3
            var blocks = Parse("0E13785634120000");
            Assert.Equal(12345.678, (double)blocks[0].Value, 9);
        }

        [Fact]
        public void Integer48BitLittleEndian()
        {
            Assert.Equal(1L, VariableDataBlock.Bit48ToInt64(new byte[] { 1, 0, 0, 0, 0, 0 }));
            Assert.Equal(1L << 40, VariableDataBlock.Bit48ToInt64(new byte[] { 0, 0, 0, 0, 0, 1 }));
            Assert.Equal(-1L, VariableDataBlock.Bit48ToInt64(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }));

            var blocks = Parse("0603010000000000");
            Assert.Equal(1L, blocks[0].Value);
        }

        [Fact]
        public void Real32Bit()
        {
            // DIF 0x05 real, VIF 0x2B power W, 1.5f little endian; the following block must still be parsed
            var blocks = Parse("052B0000C03F" + "010307");
            Assert.Equal(2, blocks.Count);
            Assert.Equal(1.5f, blocks[0].Value);
            Assert.Equal(Unit.PowerW, blocks[0].ValueInformationField.Unit);
            Assert.Equal((byte)7, blocks[1].Value);
        }

        [Fact]
        public void VifeVolt()
        {
            // VIF 0xFD, VIFE 0x47 = 10^(7-9) V, 1000 * 0.01 = 10 V
            var blocks = Parse("04FD47E8030000");
            Assert.Equal(Unit.Volt, blocks[0].ValueInformationField.Unit);
            Assert.Equal(-2, blocks[0].ValueInformationField.Multiplier);
            Assert.Equal(10d, (double)blocks[0].Value, 9);
        }

        [Fact]
        public void VifeAmpere()
        {
            // VIF 0xFD, VIFE 0x59 = 10^(9-12) A
            var blocks = Parse("04FD59E8030000");
            Assert.Equal(Unit.Ampere, blocks[0].ValueInformationField.Unit);
            Assert.Equal(1d, (double)blocks[0].Value, 9);
        }

        [Fact]
        public void VifFbIsNotDecodedWithFdTable()
        {
            // 0xFB 0x40 is not "volt" (that is only the 0xFD table)
            var blocks = Parse("04FB40E8030000");
            Assert.Equal(Unit.Unknown, blocks[0].ValueInformationField.Unit);
        }

        [Fact]
        public void Tariff()
        {
            var blocks = Parse("84100301000000");
            Assert.Equal(1, blocks[0].Tariff);
            Assert.Equal(16, blocks[0].TariffKey);

            // two DIFEs: tariff 1 (bits 0-1) and 2 (bits 2-3) -> 9
            blocks = Parse("8490200301000000");
            Assert.Equal(9, blocks[0].Tariff);
            Assert.Equal(144, blocks[0].TariffKey); // historic key format stays stable
        }

        [Fact]
        public void VariableLengthAscii()
        {
            // DIF 0x0D, LVAR 3 = ASCII, transmitted last character first; following block must still be parsed
            var blocks = Parse("0D7803434241" + "010307");
            Assert.Equal(2, blocks.Count);
            Assert.Equal("ABC", blocks[0].Value);
            Assert.Equal((byte)7, blocks[1].Value);
        }

        [Fact]
        public void TimePointTypeF()
        {
            // SU (bit 7 of the hour byte) set must still decode
            var blocks = Parse("046D248EFB16");
            Assert.Equal(new DateTime(2015, 6, 27, 14, 36, 0), blocks[0].Value);

            // IV (bit 7 of the minute byte) set -> invalid
            blocks = Parse("046DA40EFB16");
            Assert.Null(blocks[0].Value);
        }

        [Fact]
        public void TimePointTypeGZeroIsInvalid()
        {
            var blocks = Parse("026C0000" + "010307");
            Assert.Equal(2, blocks.Count);
            Assert.Null(blocks[0].Value);
            Assert.Equal((byte)7, blocks[1].Value);
        }

        [Fact]
        public void IdentificationNumberIsBcd()
        {
            var userData = Utils.StringToByteArray("785634122D2C0107010000000403" + "01000000");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, BuildLongFrame(0x72, userData)) as VariableDataFrame;

            Assert.NotNull(frame);
            Assert.Equal(12345678, frame.Identifier);
            Assert.Single(frame.DataBlocks);
        }

        [Fact]
        public void FixedDataFrame()
        {
            // ident 12345678, access 5, status 0 (BCD counters), medium water (7): low bits in byte 6, high bits in byte 7
            var userData = Utils.StringToByteArray("7856341205" + "00" + "C040" + "21436587" + "01000000");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, BuildLongFrame(0x73, userData)) as FixedDataFrame;

            Assert.NotNull(frame);
            Assert.Equal(12345678, frame.Identification);
            Assert.Equal(5, frame.AccessNumber);
            Assert.Equal(Medium.Water, frame.Medium);
            Assert.Equal(87654321, frame.Counter1);
            Assert.Equal(1, frame.Counter2);
        }

        private class FakeConnection : MBusConnection
        {
            public readonly List<byte[]> Written = new List<byte[]>();
            public readonly List<int> Timeouts = new List<int>();

            public FakeConnection(MBusConfig config) : base(config, null, NullLogger.Instance)
            {
            }

            public override bool Open() => true;
            public override bool Close() => true;
            public override bool IsConnected() => true;

            protected override Task WriteFrame(MBusFrame frame)
            {
                Written.Add(frame.ToByteFrame().ToArray());
                return Task.CompletedTask;
            }

            public override Task WriteRaw(ReadOnlyMemory<byte> data)
            {
                Written.Add(data.ToArray());
                return Task.CompletedTask;
            }

            protected override Task<MBusFrame> ReadFrame()
            {
                return Task.FromResult<MBusFrame>(new AckFrame());
            }

            protected override void SetReadTimeout(int timeout)
            {
                Timeouts.Add(timeout);
            }
        }

        [Fact]
        public async Task ResetSendsSndNke()
        {
            var connection = new FakeConnection(new MBusConfig());
            await connection.ResetDevice(5);

            Assert.Equal(new byte[] { 0x10, 0x40, 0x05, 0x45, 0x16 }, connection.Written.Single());
        }

        [Fact]
        public void DeviceTimeoutExtendsConnectionTimeout()
        {
            var connection = new FakeConnection(new MBusConfig { Timeout = 5000 });

            Assert.Equal(5000, connection.GetEffectiveTimeout(300));
            Assert.Equal(8000, connection.GetEffectiveTimeout(8000));
        }
    }
}
