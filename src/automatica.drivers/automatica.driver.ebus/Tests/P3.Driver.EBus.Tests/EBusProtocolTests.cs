using System;
using System.Collections.Generic;
using System.Linq;
using P3.Driver.EBus.Tests.Moq;
using Xunit;

namespace P3.Driver.EBus.Tests
{
    public class CrcTests
    {
        // reference CRC-8 the way the driver's table was generated (polynomial 0x07, MSB first, init 0)
        private static byte ReferenceCrc(IEnumerable<byte> data, byte poly)
        {
            byte crc = 0;
            foreach (var b in data)
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ poly) : (byte)(crc << 1);
                }
            }

            return crc;
        }

        [Fact]
        public void Crc8_EmptyInput_IsZero()
        {
            Assert.Equal(0, Crc.Crc8(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public void Crc8_SpanEqualsByteByByte()
        {
            var data = new byte[] { 0x10, 0x08, 0xB5, 0x11, 0x01, 0x01 };

            byte crc = 0;
            foreach (var b in data)
            {
                crc = Crc.Crc8(crc, b);
            }

            Assert.Equal(crc, Crc.Crc8(data));
        }

        [Fact]
        public void Crc8_TableIsConsistentForEveryByte()
        {
            // pins the table: every single-byte CRC matches a bitwise CRC-8/0x07
            for (var i = 0; i < 256; i++)
            {
                Assert.Equal(ReferenceCrc(new[] { (byte)i }, 0x07), Crc.Crc8(new[] { (byte)i }));
            }
        }

        [Fact(Skip = "BUG: the CRC table is CRC-8 polynomial 0x07, eBUS uses 0x9B: the ebusd reference telegram 10 08 B5 11 01 01 has CRC 0x89, the driver computes 0x8A (P3.Driver.EBus/Crc.cs:24)")]
        public void Crc8_MatchesTheEBusPolynomial()
        {
            var telegram = new byte[] { 0x10, 0x08, 0xB5, 0x11, 0x01, 0x01 };

            Assert.Equal(0x89, Crc.Crc8(telegram));
            Assert.Equal(ReferenceCrc(telegram, 0x9B), Crc.Crc8(telegram));
        }
    }

    public class MessageParseTests
    {
        private static byte[] Frame(byte source, byte target, byte primary, byte secondary, params byte[] data)
        {
            var body = new List<byte> { source, target, primary, secondary, (byte)data.Length };
            body.AddRange(data);
            body.Add(Message.CalculateChecksum(body.ToArray()));
            return body.ToArray();
        }

        [Fact]
        public void Parse_ValidFrame_ReadsHeaderDataAndCrc()
        {
            var frame = Frame(0x10, 0x08, 0xB5, 0x11, 0x01, 0x02, 0x03);

            var msg = Message.Parse(frame);

            Assert.Equal(0x10, msg.Source);
            Assert.Equal(0x08, msg.Target);
            Assert.Equal(0xB5, msg.PrimaryCommand);
            Assert.Equal(0x11, msg.SecondaryCommand);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, msg.Data.ToArray());
            Assert.Equal(frame[^1], msg.Crc16);
        }

        [Fact]
        public void Parse_TrailingAckAndSyn_AreIgnored()
        {
            var frame = Frame(0x10, 0x08, 0xB5, 0x11, 0x01).Concat(new byte[] { 0x00, 0xAA }).ToArray();

            var msg = Message.Parse(frame);

            Assert.Equal(new byte[] { 0x01 }, msg.Data.ToArray());
        }

        [Fact]
        public void Parse_DataIsACopy()
        {
            var frame = Frame(0x10, 0x08, 0xB5, 0x11, 0x42);

            var msg = Message.Parse(frame);
            frame[5] = 0x00;

            Assert.Equal(0x42, msg.Data.Span[0]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public void Parse_TooShort_Throws(int length)
        {
            Assert.Throws<ArgumentException>(() => Message.Parse(new byte[length]));
        }

        [Fact]
        public void Parse_LengthBeyondTheBuffer_Throws()
        {
            var frame = new byte[] { 0x10, 0x08, 0xB5, 0x11, 0x10, 0x01, 0x02 };

            var ex = Assert.Throws<ArgumentException>(() => Message.Parse(frame));
            Assert.Contains("invalid", ex.Message);
        }

        [Fact]
        public void Parse_WrongCrc_Throws()
        {
            var frame = Frame(0x10, 0x08, 0xB5, 0x11, 0x01, 0x02);
            frame[^1] ^= 0xFF;

            var ex = Assert.Throws<ArgumentException>(() => Message.Parse(frame));
            Assert.Contains("CRC", ex.Message);
        }

        [Fact]
        public void Parse_FlippedDataBit_FailsTheCrc()
        {
            var frame = Frame(0x10, 0x08, 0xB5, 0x11, 0x01, 0x02);
            frame[5] ^= 0x01;

            Assert.Throws<ArgumentException>(() => Message.Parse(frame));
        }

        [Fact(Skip = "BUG: the length check is dataLen + 4 > Length, a frame cut off right before the CRC byte reads buffer[5 + dataLen] and throws IndexOutOfRangeException instead of ArgumentException (P3.Driver.EBus/Message.cs:37)")]
        public void Parse_MissingCrcByte_ThrowsArgumentException()
        {
            var frame = Frame(0x10, 0x08, 0xB5, 0x11, 0x01, 0x02);
            var withoutCrc = frame.Take(frame.Length - 1).ToArray();

            Assert.Throws<ArgumentException>(() => Message.Parse(withoutCrc));
        }
    }

    public class SynBufferTests
    {
        private static readonly byte[] Frame1 = { 0x05, 0x03, 0x09, 0x07, 0x03, 0x55, 0x00, 0x50, 0x80, 0x00, 0x62, 0xff, 0xa3, 0xff, 0xaa };

        private static List<byte[]> Feed(EBusMoq bus, IEnumerable<byte> bytes)
        {
            var frames = new List<byte[]>();
            foreach (var b in bytes)
            {
                var msg = bus.Write(new[] { b });
                if (msg != null)
                {
                    frames.Add(msg);
                }
            }

            return frames;
        }

        [Fact]
        public void TwoFramesBackToBack_AreReturnedSeparately()
        {
            var second = new byte[] { 0x10, 0xfe, 0xb5, 0x05, 0x04, 0x27, 0xa9, 0x15, 0xaa };

            var frames = Feed(new EBusMoq(null), Frame1.Concat(second));

            Assert.Equal(2, frames.Count);
            Assert.Equal(Frame1, frames[0]);
            Assert.Equal(second, frames[1]);
        }

        [Fact]
        public void IdleSynBytes_AreSingleByteFrames()
        {
            var frames = Feed(new EBusMoq(null), new byte[] { 0xaa, 0xaa, 0xaa });

            Assert.Equal(3, frames.Count);
            Assert.All(frames, f => Assert.Equal(new byte[] { 0xaa }, f));
        }

        [Fact]
        public void BytesWithoutSyn_AreBuffered()
        {
            var bus = new EBusMoq(null);

            Assert.Empty(Feed(bus, Frame1.Take(Frame1.Length - 1)));
            var frames = Feed(bus, new byte[] { 0xaa });

            Assert.Equal(Frame1, Assert.Single(frames));
        }

        [Fact]
        public void BufferIsClearedAfterAFrame()
        {
            var bus = new EBusMoq(null);
            Feed(bus, Frame1);

            var frames = Feed(bus, new byte[] { 0x01, 0x02, 0xaa });

            Assert.Equal(new byte[] { 0x01, 0x02, 0xaa }, Assert.Single(frames));
        }

        [Fact]
        public void MoreThan1024BytesWithoutSyn_WrapsAndKeepsOnlyTheNewBytes()
        {
            var bus = new EBusMoq(null);
            Assert.Empty(Feed(bus, Enumerable.Repeat((byte)0x55, 1024)));

            var frames = Feed(bus, Frame1);

            Assert.Equal(Frame1, Assert.Single(frames));
        }

        [Fact]
        public void ParsedFromTheStream_AFrameBuiltWithTheDriverCrc()
        {
            var body = new byte[] { 0x10, 0x08, 0xB5, 0x11, 0x01, 0x01 };
            var wire = body.Concat(new[] { Message.CalculateChecksum(body), (byte)0xaa }).ToArray();

            var frame = Assert.Single(Feed(new EBusMoq(null), wire));
            var msg = Message.Parse(frame);

            Assert.Equal(0xB5, msg.PrimaryCommand);
            Assert.Equal(new byte[] { 0x01 }, msg.Data.ToArray());
        }
    }
}
