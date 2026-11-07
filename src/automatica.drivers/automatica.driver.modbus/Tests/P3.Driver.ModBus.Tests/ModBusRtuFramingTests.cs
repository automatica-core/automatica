using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriver.Master.Rtu;
using RJCP.IO.Ports;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    /// <summary>
    /// RTU crc, frame building and frame reading over in-memory streams (no serial port needed)
    /// </summary>
    public class ModBusRtuFramingTests
    {
        #region helpers

        private static byte[] Hex(string hex)
        {
            return hex.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(h => Convert.ToByte(h, 16)).ToArray();
        }

        private static byte[] WithCrc(params byte[] frame)
        {
            return frame.Concat(ModbusUtility.CalculateCrc(frame)).ToArray();
        }

        /// <summary>
        /// Bitwise CRC-16/MODBUS (poly 0xA001 reflected, init 0xFFFF), independent of the table implementation
        /// </summary>
        private static ushort ReferenceCrc(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach (var b in data)
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                }
            }
            return crc;
        }

        /// <summary>
        /// A serial port like stream: every read returns the next scripted chunk; an empty chunk returns 0 (read timeout),
        /// a null chunk throws <see cref="TimeoutException"/> like SerialPortStream does
        /// </summary>
        private sealed class ScriptedSerialStream : Stream
        {
            private readonly Queue<byte[]> _chunks;
            private byte[] _current = Array.Empty<byte>();
            private int _offset;

            public ScriptedSerialStream(params byte[][] chunks)
            {
                _chunks = new Queue<byte[]>(chunks);
            }

            public int RemainingBytes => _current.Length - _offset + _chunks.Where(c => c != null).Sum(c => c.Length);

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_offset >= _current.Length)
                {
                    if (_chunks.Count == 0)
                    {
                        return 0;
                    }

                    _current = _chunks.Dequeue();
                    _offset = 0;
                    if (_current == null)
                    {
                        _current = Array.Empty<byte>();
                        throw new TimeoutException();
                    }
                }

                var n = Math.Min(count, _current.Length - _offset);
                Array.Copy(_current, _offset, buffer, offset, n);
                _offset += n;
                return n;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private static ScriptedSerialStream Chunked(byte[] data, int chunkSize)
        {
            return new ScriptedSerialStream(data.Chunk(chunkSize).ToArray());
        }

        private static async Task<byte[]> ReadFrame(Stream stream, ModBusFunction function, int count, int timeoutMs = 2000)
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            return await ModBusFrameReader.ReadRtuFrameAsync(stream, function, count, cts.Token);
        }

        /// <summary>
        /// Exposes the protected frame builders. Created without running the constructor, so no serial port is touched.
        /// </summary>
        private sealed class RtuFrameBuilder : ModBusMasterRtuDriver
        {
            private RtuFrameBuilder() : base(new ModBusMasterRtuConfig(), new EmptyTelegramMonitorInstance())
            {
            }

            public static RtuFrameBuilder Create() => (RtuFrameBuilder)RuntimeHelpers.GetUninitializedObject(typeof(RtuFrameBuilder));

            public byte[] Read(byte slave, int address, int count, ModBusFunction function) => BuildReadFrame(slave, address, count, function);

            public byte[] Write(byte slave, ModBusFunction function, params byte[] pdu) => UpdateWriteFrame(BuildHeaderFromDataFrame(pdu, slave, function));

            public short RequestLength(byte[] frame) => GetRequestLength(frame);
        }

        #endregion

        #region crc

        [Theory]
        [InlineData("01 03 00 00 00 01", "84 0A")]
        [InlineData("11 03 00 6B 00 03", "76 87")]
        [InlineData("01 04 00 00 00 0A", "70 0D")]
        [InlineData("01 05 00 00 FF 00", "8C 3A")]
        [InlineData("01 06 00 01 00 03", "98 0B")]
        [InlineData("", "FF FF")]
        public void Crc_KnownFrames_LowByteFirst(string frame, string crc)
        {
            Assert.Equal(Hex(crc), ModbusUtility.CalculateCrc(Hex(frame)));
        }

        [Fact]
        public void Crc_RandomData_MatchesTheBitwiseReference()
        {
            var random = new Random(4711);
            for (var length = 1; length < 300; length += 7)
            {
                var data = new byte[length];
                random.NextBytes(data);

                var crc = ModbusUtility.CalculateCrc(data);
                var reference = ReferenceCrc(data);

                Assert.Equal(new[] { (byte)reference, (byte)(reference >> 8) }, crc);
            }
        }

        [Fact]
        public void Crc_OverFrameAndCrc_IsZero()
        {
            var frame = WithCrc(Hex("11 03 06 AE 41 56 52 43 40"));

            Assert.Equal(new byte[] { 0, 0 }, ModbusUtility.CalculateCrc(frame));
        }

        [Fact]
        public void Crc_EverySingleBitFlip_IsDetected()
        {
            var frame = Hex("01 10 00 01 00 02 04 00 0A 01 02");
            var crc = ModbusUtility.CalculateCrc(frame);

            for (var i = 0; i < frame.Length * 8; i++)
            {
                var corrupted = (byte[])frame.Clone();
                corrupted[i / 8] ^= (byte)(1 << (i % 8));
                Assert.NotEqual(crc, ModbusUtility.CalculateCrc(corrupted));
            }
        }

        [Fact]
        public void Crc_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ModbusUtility.CalculateCrc(null));
            Assert.Throws<ArgumentNullException>(() => ModbusUtility.CalculateLrc(null));
        }

        [Theory]
        [InlineData("01 03 00 00 00 0A", 0xF2)]
        [InlineData("11 03 00 6B 00 03", 0x7E)]
        [InlineData("00", 0x00)]
        public void Lrc_KnownFrames(string frame, int lrc)
        {
            Assert.Equal((byte)lrc, ModbusUtility.CalculateLrc(Hex(frame)));
        }

        [Fact]
        public void Lrc_SumOfDataAndLrc_IsZero()
        {
            var data = Hex("F7 03 13 89 00 0A");
            var lrc = ModbusUtility.CalculateLrc(data);

            Assert.Equal(0, (byte)(data.Sum(b => b) + lrc));
        }

        #endregion

        #region frame building

        [Fact]
        public void BuildReadFrame_SpecExample_HasAddressCountAndCrc()
        {
            var frame = RtuFrameBuilder.Create().Read(0x11, 0x006B, 3, ModBusFunction.ReadHoldingRegisters);

            Assert.Equal(Hex("11 03 00 6B 00 03 76 87"), frame);
        }

        [Theory]
        [InlineData(ModBusFunction.ReadCoils, "01 01 00 13 00 25")]
        [InlineData(ModBusFunction.ReadDiscreteInputs, "01 02 00 13 00 25")]
        [InlineData(ModBusFunction.ReadInputRegisters, "01 04 00 13 00 25")]
        public void BuildReadFrame_EveryReadFunction_EndsWithAValidCrc(ModBusFunction function, string pdu)
        {
            var frame = RtuFrameBuilder.Create().Read(0x01, 0x0013, 0x25, function);

            Assert.Equal(WithCrc(Hex(pdu)), frame);
        }

        [Fact]
        public void BuildReadFrame_HighAddressAndCount_AreBigEndian()
        {
            var frame = RtuFrameBuilder.Create().Read(0xF7, 0xABCD, 0x0102, ModBusFunction.ReadHoldingRegisters);

            Assert.Equal(WithCrc(Hex("F7 03 AB CD 01 02")), frame);
        }

        [Fact]
        public void BuildWriteFrame_SingleRegister_IsSlaveIdPduAndCrc()
        {
            var frame = RtuFrameBuilder.Create().Write(0x01, ModBusFunction.WriteSingleRegister, Hex("06 00 01 00 03"));

            Assert.Equal(Hex("01 06 00 01 00 03 98 0B"), frame);
        }

        [Fact]
        public void BuildWriteFrame_SingleCoil_IsSlaveIdPduAndCrc()
        {
            var frame = RtuFrameBuilder.Create().Write(0x01, ModBusFunction.WriteSingleCoil, Hex("05 00 00 FF 00"));

            Assert.Equal(Hex("01 05 00 00 FF 00 8C 3A"), frame);
        }

        [Fact]
        public void RtuRequestLength_IsNotUsed()
        {
            Assert.Equal(-1, RtuFrameBuilder.Create().RequestLength(new byte[8]));
        }

        #endregion

        #region response length

        [Theory]
        [InlineData(ModBusFunction.ReadHoldingRegisters, 1, 7)]
        [InlineData(ModBusFunction.ReadHoldingRegisters, 125, 255)]
        [InlineData(ModBusFunction.ReadInputRegisters, 2, 9)]
        [InlineData(ModBusFunction.ReadCoils, 1, 6)]
        [InlineData(ModBusFunction.ReadCoils, 8, 6)]
        [InlineData(ModBusFunction.ReadCoils, 9, 7)]
        [InlineData(ModBusFunction.ReadDiscreteInputs, 2000, 255)]
        [InlineData(ModBusFunction.WriteSingleCoil, 1, 8)]
        [InlineData(ModBusFunction.WriteSingleRegister, 1, 8)]
        [InlineData(ModBusFunction.WriteMultipleCoils, 20, 8)]
        [InlineData(ModBusFunction.WriteMultipleRegisters, 4, 8)]
        public void ResponseLength_PerFunction_IncludesSlaveFunctionAndCrc(ModBusFunction function, int count, int expected)
        {
            Assert.Equal(expected, ModBusFrameReader.GetRtuResponseLength(function, count));
        }

        #endregion

        #region frame reading

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(100)]
        public async Task ReadFrame_HoldingRegisters_AnyChunking_ReturnsTheCompleteFrame(int chunkSize)
        {
            var response = WithCrc(Hex("01 03 04 00 7B FF FB"));

            var frame = await ReadFrame(Chunked(response, chunkSize), ModBusFunction.ReadHoldingRegisters, 2);

            Assert.Equal(response, frame);
        }

        [Fact]
        public async Task ReadFrame_Coils_ReturnsTheCompleteFrame()
        {
            var response = WithCrc(Hex("01 01 03 CD 6B 05"));

            Assert.Equal(response, await ReadFrame(Chunked(response, 2), ModBusFunction.ReadCoils, 19));
        }

        [Fact]
        public async Task ReadFrame_WriteMultipleRegistersEcho_ReturnsTheCompleteFrame()
        {
            var response = WithCrc(Hex("01 10 00 01 00 02"));

            Assert.Equal(response, await ReadFrame(Chunked(response, 3), ModBusFunction.WriteMultipleRegisters, 2));
        }

        [Fact]
        public async Task ReadFrame_SerialReadTimeoutsInBetween_AreRetried()
        {
            var response = WithCrc(Hex("01 03 02 12 34"));

            // read timeouts (0 bytes and TimeoutException) between the parts, like a slow serial line
            var stream = new ScriptedSerialStream(response.Take(2).ToArray(), Array.Empty<byte>(), null, response.Skip(2).Take(1).ToArray(),
                null, Array.Empty<byte>(), response.Skip(3).ToArray());

            Assert.Equal(response, await ReadFrame(stream, ModBusFunction.ReadHoldingRegisters, 1));
        }

        [Fact]
        public async Task ReadFrame_ExceptionReplyByteByByte_IsReturnedAfterFiveBytes()
        {
            var response = WithCrc(Hex("0A 84 02"));
            var trailing = Hex("AA BB CC");
            var stream = Chunked(response.Concat(trailing).ToArray(), 1);

            var frame = await ReadFrame(stream, ModBusFunction.ReadInputRegisters, 10);

            Assert.Equal(response, frame);
            // nothing after the exception reply was consumed
            Assert.Equal(trailing.Length, stream.RemainingBytes);
        }

        [Fact]
        public async Task ReadFrame_ExceptionReplyWithInvalidCrc_Throws()
        {
            var response = WithCrc(Hex("01 83 02"));
            response[3] ^= 0x01;

            await Assert.ThrowsAsync<InvalidDataException>(() => ReadFrame(Chunked(response, 5), ModBusFunction.ReadHoldingRegisters, 2));
        }

        [Fact]
        public async Task ReadFrame_CorruptedPayload_Throws()
        {
            var response = WithCrc(Hex("01 03 04 00 7B FF FB"));
            response[4] ^= 0x10;

            await Assert.ThrowsAsync<InvalidDataException>(() => ReadFrame(Chunked(response, 4), ModBusFunction.ReadHoldingRegisters, 2));
        }

        [Fact]
        public async Task ReadFrame_TwoFramesBackToBack_ReadsOnlyTheFirst()
        {
            var first = WithCrc(Hex("01 06 00 01 00 03"));
            var second = WithCrc(Hex("01 03 02 00 2A"));
            var stream = Chunked(first.Concat(second).ToArray(), 64);

            Assert.Equal(first, await ReadFrame(stream, ModBusFunction.WriteSingleRegister, 1));
            Assert.Equal(second, await ReadFrame(stream, ModBusFunction.ReadHoldingRegisters, 1));
        }

        [Fact]
        public async Task ReadFrame_NoDataAtAll_TimesOut()
        {
            var started = DateTime.UtcNow;

            await Assert.ThrowsAsync<TimeoutException>(() => ReadFrame(new ScriptedSerialStream(), ModBusFunction.ReadHoldingRegisters, 1, 150));

            Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task ReadFrame_AlreadyCancelled_ThrowsTimeoutWithoutReading()
        {
            var response = WithCrc(Hex("01 03 02 12 34"));
            var stream = Chunked(response, 100);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<TimeoutException>(() => ModBusFrameReader.ReadRtuFrameAsync(stream, ModBusFunction.ReadHoldingRegisters, 1, cts.Token));
            Assert.Equal(response.Length, stream.RemainingBytes);
        }

        [Fact]
        public async Task ReadExactly_FillsTheBufferOverSeveralReads()
        {
            var stream = Chunked(Enumerable.Range(0, 20).Select(i => (byte)i).ToArray(), 3);
            var buffer = new byte[20];

            Assert.True(await ModBusFrameReader.ReadExactlyAsync(stream, buffer, CancellationToken.None));
            Assert.Equal(Enumerable.Range(0, 20).Select(i => (byte)i), buffer);
        }

        [Fact]
        public async Task ReadExactly_StreamEndsEarly_ReturnsFalse()
        {
            var stream = Chunked(new byte[] { 1, 2, 3 }, 2);

            Assert.False(await ModBusFrameReader.ReadExactlyAsync(stream, new byte[6], CancellationToken.None));
        }

        #endregion

        #region parity and stop bits

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("x")]
        [InlineData("parity")]
        public void Parity_Unknown_FallsBackToNone(string value)
        {
            Assert.Equal(Parity.None, ModBusMasterRtuDriver.FromString(value));
        }

        [Theory]
        [InlineData(" even ", Parity.Even)]
        [InlineData("E", Parity.Even)]
        [InlineData("O", Parity.Odd)]
        [InlineData("MarkParity", Parity.Mark)]
        [InlineData("S", Parity.Space)]
        [InlineData("SpaceParity", Parity.Space)]
        [InlineData("N", Parity.None)]
        public void Parity_CaseAndWhitespace_AreIgnored(string value, Parity expected)
        {
            Assert.Equal(expected, ModBusMasterRtuDriver.FromString(value));
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(3d)]
        [InlineData(-1d)]
        public void StopBits_Unknown_FallsBackToOne(double value)
        {
            Assert.Equal(StopBits.One, ModBusMasterRtuDriver.FromString(value));
        }

        #endregion
    }
}
