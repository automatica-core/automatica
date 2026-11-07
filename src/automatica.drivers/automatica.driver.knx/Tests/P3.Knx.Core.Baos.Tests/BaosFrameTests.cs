using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Knx.Core.Baos.Driver;
using P3.Knx.Core.Baos.Driver.Exceptions;
using P3.Knx.Core.Baos.Driver.Frames;
using Xunit;

namespace P3.Knx.Core.Baos.Tests
{
    /// <summary>
    /// FT1.2 framing of the BAOS serial protocol: fixed length (short) frames, variable length (long) frames, checksums
    /// and the datapoint value responses / indications.
    /// </summary>
    public class BaosFrameTests
    {
        private static byte[] LongFrameBytes(byte control, params byte[] userData)
        {
            var frame = new byte[7 + userData.Length];
            frame[0] = 0x68;
            frame[1] = (byte)(userData.Length + 1);
            frame[2] = (byte)(userData.Length + 1);
            frame[3] = 0x68;
            frame[4] = control;
            userData.CopyTo(frame, 5);
            frame[5 + userData.Length] = (byte)(control + userData.Sum(a => a));
            frame[6 + userData.Length] = 0x16;
            return frame;
        }

        private static LongFrame Long(byte control, params byte[] userData) =>
            Assert.IsType<LongFrame>(BaosFrame.FromByteArray(BaosFrameType.LongFrame, NullLogger.Instance, LongFrameBytes(control, userData)));

        // ---- short frames ----

        [Fact]
        public void ResetFrame_IsStartControlChecksumEnd()
        {
            var frame = ShortFrame.CreateResetFrame();

            Assert.Equal(BaosFrameType.ShortFrame, frame.FrameType);
            Assert.Equal(new byte[] { 0x10, 0x40, 0x40, 0x16 }, frame.ToByteFrame().ToArray());
            Assert.Equal(BaosFrame.ShortFrameLength, frame.ToByteFrame().Length);
        }

        [Theory]
        [InlineData(0xC0)]
        [InlineData(0x40)]
        [InlineData(0x00)]
        [InlineData(0xFF)]
        public void ShortFrame_ValidChecksum_RoundTrips(byte control)
        {
            var bytes = new byte[] { 0x10, control, control, 0x16 };

            var frame = Assert.IsType<ShortFrame>(BaosFrame.FromByteArray(BaosFrameType.ShortFrame, NullLogger.Instance, bytes));

            Assert.Equal(control, frame.ControlField);
            Assert.True(frame.ChecksumCheck());
            Assert.Equal(bytes, frame.ToByteFrame().ToArray());
            Assert.Equal(bytes, frame.RawData.ToArray());
        }

        [Fact]
        public void ShortFrame_InvalidChecksum_IsRejected()
        {
            Assert.Null(BaosFrame.FromByteArray(BaosFrameType.ShortFrame, NullLogger.Instance, new byte[] { 0x10, 0xC0, 0xC1, 0x16 }));
        }

        [Fact]
        public void ReadShortFrame_ValidFrame_ReadsExactlyThreeBytesAfterTheStartByte()
        {
            var stream = new MemoryStream(new byte[] { 0x40, 0x40, 0x16, 0xE5, 0x68 });

            var frame = BaosSerial.ReadShortFrame(stream.Read, NullLogger.Instance);

            Assert.IsType<ShortFrame>(frame);
            Assert.Equal(0x40, frame.ControlField);
            Assert.Equal(3, stream.Position);
        }

        [Fact]
        public void ReadShortFrame_InvalidChecksum_IsRejected()
        {
            var stream = new MemoryStream(new byte[] { 0xC0, 0x41, 0x16 });

            Assert.Null(BaosSerial.ReadShortFrame(stream.Read, NullLogger.Instance));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ReadShortFrame_PartialRead_IsRejected(int available)
        {
            var stream = new MemoryStream(new byte[] { 0xC0, 0xC0, 0x16 }.Take(available).ToArray());

            Assert.Null(BaosSerial.ReadShortFrame(stream.Read, NullLogger.Instance));
        }

        [Fact]
        public void ReadShortFrame_ReaderReturnsLessThanRequested_IsRejected()
        {
            // a serial read can return fewer bytes than requested
            int Read(byte[] buffer, int offset, int count)
            {
                buffer[offset] = 0xC0;
                return 1;
            }

            Assert.Null(BaosSerial.ReadShortFrame(Read, NullLogger.Instance));
        }

        // ---- single character ----

        [Fact]
        public void AckFrame_IsTheSingleCharacterE5()
        {
            var ack = new AckFrame();

            Assert.Equal(BaosFrameType.SingleChar, ack.FrameType);
            Assert.Equal(new byte[] { BaosFrame.SingleCharFrame }, ack.ToByteFrame().ToArray());
            Assert.IsType<AckFrame>(BaosFrame.FromByteArray(BaosFrameType.SingleChar, NullLogger.Instance, new byte[] { 0xE5 }));
        }

        // ---- long frames ----

        [Fact]
        public void Create_LongFrame_HasTheFt12Layout()
        {
            var userData = new byte[] { 0xF0, 0x06, 0x00, 0x05, 0x00, 0x01, 0x00, 0x05, 0x03, 0x01, 0x01 };

            var frame = BaosFrame.Create(userData);
            var bytes = frame.ToByteFrame().ToArray();

            Assert.Equal(BaosFrameType.LongFrame, frame.FrameType);
            Assert.Equal(7 + userData.Length, bytes.Length);
            Assert.Equal(0x68, bytes[0]);
            Assert.Equal(userData.Length + 1, bytes[1]);
            Assert.Equal(bytes[1], bytes[2]);
            Assert.Equal(0x68, bytes[3]);
            Assert.Contains(bytes[4], new byte[] { 0x53, 0x73 }); // odd / even frame control
            Assert.Equal(userData, bytes.Skip(5).Take(userData.Length).ToArray());
            Assert.Equal((byte)(bytes[4] + userData.Sum(a => a)), bytes[^2]);
            Assert.Equal(0x16, bytes[^1]);
        }

        [Fact]
        public void LongFrame_ToByteFrameAndBack_KeepsTheUserData()
        {
            var userData = new byte[] { 0xF0, 0x05, 0x00, 0x01, 0x00, 0x02, 0x00 };
            var bytes = BaosFrame.Create(userData).ToByteFrame().ToArray();

            var parsed = Assert.IsType<LongFrame>(BaosFrame.FromByteArray(BaosFrameType.LongFrame, NullLogger.Instance, bytes));

            Assert.Equal(userData, parsed.UserData.ToArray());
            Assert.True(parsed.ChecksumCheck());
            Assert.Equal(bytes, parsed.ToByteFrame().ToArray());
        }

        [Fact]
        public void LongFrame_ChecksumIsTheSumModulo256()
        {
            var frame = Long(0xF3, 0xFF, 0xFF, 0xFF, 0x10);

            Assert.True(frame.ChecksumCheck());
            Assert.Equal((byte)((0xF3 + 0xFF * 3 + 0x10) % 256), frame.ToByteFrame().ToArray()[^2]);
        }

        [Fact]
        public void LongFrame_CorruptedUserData_IsRejected()
        {
            var bytes = LongFrameBytes(0xF3, 0xF0, 0x85, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x10, 0x01, 0x01);
            bytes[10] ^= 0x01;

            Assert.Null(BaosFrame.FromByteArray(BaosFrameType.LongFrame, NullLogger.Instance, bytes));
        }

        [Fact]
        public void FromSpan_HeaderAndData_AreJoined()
        {
            var bytes = LongFrameBytes(0xF3, 0xF0, 0xC1, 0x00, 0x03, 0x00, 0x01, 0x00, 0x03, 0x10, 0x01, 0x01);

            var frame = BaosFrame.FromSpan(BaosFrameType.LongFrame, NullLogger.Instance, bytes.AsSpan(0, 4), bytes.AsSpan(4));

            Assert.IsType<LongFrame>(frame);
            Assert.Equal(bytes.Skip(5).Take(bytes.Length - 7).ToArray(), frame.UserData.ToArray());
        }

        // ---- datapoint values ----

        [Fact]
        public void ParseDatapointValues_DifferentLengths_AreSplitByTheirLengthByte()
        {
            // GetDatapointValue.Res: start 1, 3 datapoints: #1 = 1 byte, #258 = 2 bytes, #1000 = 4 bytes
            var frame = Long(0xF3,
                0xF0, 0x85, 0x00, 0x01, 0x00, 0x03,
                0x00, 0x01, 0x10, 0x01, 0x01,
                0x01, 0x02, 0x11, 0x02, 0x0C, 0x1A,
                0x03, 0xE8, 0x00, 0x04, 0x41, 0xA8, 0x00, 0x00);

            var values = BaosDriver.ParseDatapointValues(frame).ToList();

            Assert.Equal(new ushort[] { 1, 258, 1000 }, values.Select(a => a.DatapointId));
            Assert.Equal(new byte[] { 0x10, 0x11, 0x00 }, values.Select(a => a.State));
            Assert.Equal(new byte[] { 1, 2, 4 }, values.Select(a => a.Length));
            Assert.Equal(new byte[] { 0x01 }, values[0].Data.ToArray());
            Assert.Equal(new byte[] { 0x0C, 0x1A }, values[1].Data.ToArray());
            Assert.Equal(new byte[] { 0x41, 0xA8, 0x00, 0x00 }, values[2].Data.ToArray());
        }

        [Fact]
        public void ParseDatapointValues_DatapointValueIndication_IsParsed()
        {
            // DatapointValue.Ind (0xC1) has the same layout as the response
            var frame = Long(0xF3, 0xF0, 0xC1, 0x00, 0x05, 0x00, 0x01, 0x00, 0x05, 0x03, 0x01, 0x01);

            var value = Assert.Single(BaosDriver.ParseDatapointValues(frame));

            Assert.Equal(5, value.DatapointId);
            Assert.Equal(0x03, value.State);
            Assert.Equal(new byte[] { 0x01 }, value.Data.ToArray());
        }

        [Theory]
        [InlineData(0x02, ErrorCodes.NoElementFound)]
        [InlineData(0x07, ErrorCodes.BadId)]
        [InlineData(0x0B, ErrorCodes.ObjectServerBusy)]
        public void ParseDatapointValues_NoDatapoints_ThrowsTheErrorCode(byte errorCode, ErrorCodes expected)
        {
            var frame = Long(0xF3, 0xF0, 0x85, 0x00, 0x01, 0x00, 0x00, errorCode);

            var ex = Assert.Throws<RequestException>(() => BaosDriver.ParseDatapointValues(frame));

            Assert.Equal(expected, ex.ErrorCode);
        }

        [Fact]
        public void ParseDatapointValues_Null_Throws()
        {
            Assert.Throws<ArgumentException>(() => BaosDriver.ParseDatapointValues(null));
        }

        [Fact]
        public void ParseDatapointValues_FrameShorterThanItsDatapointCount_ThrowsInsteadOfReadingGarbage()
        {
            // claims 2 datapoints, contains one
            var frame = Long(0xF3, 0xF0, 0x85, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x10, 0x01, 0x01);

            Assert.ThrowsAny<ArgumentException>(() => BaosDriver.ParseDatapointValues(frame).ToList());
        }

        [Fact]
        public void ParseDatapointValues_LengthByteBeyondTheFrame_Throws()
        {
            var frame = Long(0xF3, 0xF0, 0x85, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x10, 0x08, 0x01);

            Assert.ThrowsAny<ArgumentException>(() => BaosDriver.ParseDatapointValues(frame).ToList());
        }

        [Fact]
        public void BaosDriver_StartWithoutSerialPort_ReturnsFalse()
        {
            var driver = new BaosDriver("automatica-no-such-port", NullLogger.Instance, null);

            Assert.False(driver.Start().GetAwaiter().GetResult());
        }
    }
}
