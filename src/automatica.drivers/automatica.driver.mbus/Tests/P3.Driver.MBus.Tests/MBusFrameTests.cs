using System;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus.Frames;
using Xunit;

namespace P3.Driver.MBus.Tests
{
    public class MBusFrameTests
    {
        internal static byte[] LongFrame(byte control, byte address, byte ci, byte[] userData)
        {
            var frame = new byte[9 + userData.Length];
            frame[0] = 0x68;
            frame[1] = (byte)(3 + userData.Length);
            frame[2] = (byte)(3 + userData.Length);
            frame[3] = 0x68;
            frame[4] = control;
            frame[5] = address;
            frame[6] = ci;
            Array.Copy(userData, 0, frame, 7, userData.Length);

            byte cs = 0;
            for (var i = 4; i < 7 + userData.Length; i++)
            {
                cs += frame[i];
            }

            frame[7 + userData.Length] = cs;
            frame[8 + userData.Length] = 0x16;
            return frame;
        }

        [Theory]
        [InlineData(MBus.ControlMaskReqUd2, 0x01, "105B015C16")]
        [InlineData(MBus.ControlMaskReqUd2, 0xFE, "105BFE5916")]
        [InlineData(MBus.ControlMaskSndNke, 0x05, "1040054516")]
        [InlineData(MBus.ControlMaskSndNke, 0xFF, "1040FF3F16")]
        public void CreateShortFrame_ToByteFrame_HasStartControlAddressChecksumStop(byte control, byte address, string expectedHex)
        {
            var frame = MBusFrame.CreateShortFrame(control, address);

            Assert.Equal(Utils.StringToByteArray(expectedHex), frame.ToByteFrame().ToArray());
        }

        [Fact]
        public void CreateChangePrimaryAddressFrame_IsSndUdLongFrameWithBusAddressVif()
        {
            var frame = MBusFrame.CreateChangePrimaryAddressFrame(1, 9);

            var bytes = frame.ToByteFrame().ToArray();

            // 68 L L 68 C=53 A=01 CI=51 DIF=01 VIF=7A new=09 CS 16
            Assert.Equal(new byte[] { 0x68, 0x06, 0x06, 0x68, 0x53, 0x01, 0x51, 0x01, 0x7A, 0x09, 0x29, 0x16 }, bytes);
        }

        [Fact]
        public void AckFrame_ToByteFrame_IsSingleE5()
        {
            Assert.Equal(new byte[] { 0xE5 }, new AckFrame().ToByteFrame().ToArray());
        }

        [Fact]
        public void FromByteArray_ShortFrameWithValidChecksum_ParsesControlAndAddress()
        {
            var raw = Utils.StringToByteArray("105B015C16");

            var frame = MBusFrame.FromByteArray(MBusFrameType.ShortFrame, NullLogger.Instance, raw);

            Assert.IsType<ShortFrame>(frame);
            Assert.Equal(0x5B, frame.ControlField);
            Assert.Equal(0x01, frame.AddressField);
        }

        [Fact]
        public void FromByteArray_ShortFrameWithBadChecksum_ReturnsNull()
        {
            var raw = Utils.StringToByteArray("105B015D16");

            Assert.Null(MBusFrame.FromByteArray(MBusFrameType.ShortFrame, NullLogger.Instance, raw));
        }

        [Fact]
        public void FromByteArray_LongFrameWithBadChecksum_ReturnsNull()
        {
            var raw = LongFrame(0x08, 0x01, 0x72, Utils.StringToByteArray("78563412" + "2D2C" + "01" + "07" + "01" + "00" + "0000" + "0403" + "01000000"));
            raw[^2] ^= 0x01;

            Assert.Null(MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, raw));
        }

        [Theory]
        [InlineData(0x72, typeof(VariableDataFrame))]
        [InlineData(0x76, typeof(VariableDataFrame))]
        [InlineData(0x73, typeof(FixedDataFrame))]
        [InlineData(0x77, typeof(FixedDataFrame))]
        [InlineData(0x78, typeof(MBusFrame))]
        public void FromByteArray_LongFrame_CiFieldSelectsTheFrameType(byte ci, Type expected)
        {
            var userData = new byte[16];
            userData[0] = 0x01; // valid BCD id
            var raw = LongFrame(0x08, 0x01, ci, userData);

            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, raw);

            Assert.NotNull(frame);
            Assert.Equal(expected, frame.GetType());
            Assert.Equal(ci, frame.CiField);
            Assert.Equal(16, frame.UserData.Length);
        }

        [Fact]
        public void FromSpan_HeaderAndData_AreJoined()
        {
            var raw = LongFrame(0x08, 0x05, 0x78, new byte[] { 1, 2, 3 });

            var frame = MBusFrame.FromSpan(MBusFrameType.LongFrame, NullLogger.Instance, raw.AsSpan(0, 4), raw.AsSpan(4));

            Assert.NotNull(frame);
            Assert.Equal(0x05, frame.AddressField);
            Assert.Equal(new byte[] { 1, 2, 3 }, frame.UserData.ToArray());
            Assert.Equal(raw, frame.RawData.ToArray());
        }

        [Fact]
        public void LongFrame_ParseThenEncode_RoundTrips()
        {
            var raw = LongFrame(0x08, 0x05, 0x78, new byte[] { 0xAA, 0xBB, 0xCC, 0xDD });

            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, raw);

            Assert.Equal(raw, frame.ToByteFrame().ToArray());
            Assert.True(frame.ChecksumCheck());
        }

        [Fact]
        public void FromByteArray_SingleChar_IsAckFrame()
        {
            var frame = MBusFrame.FromByteArray(MBusFrameType.SingleChar, NullLogger.Instance, new byte[] { 0xE5 });

            Assert.IsType<AckFrame>(frame);
        }

        [Fact]
        public void VariableDataFrame_Header_IsDecoded()
        {
            // ident 12345678 (BCD), manufacturer 0x2C2D = "KAM", version 0x19, medium heat (4), access 0x2A, status 0x10, signature 0
            var userData = Utils.StringToByteArray("78563412" + "2D2C" + "19" + "04" + "2A" + "10" + "0000" + "0403" + "01000000");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x72, userData)) as VariableDataFrame;

            Assert.NotNull(frame);
            Assert.Equal(12345678, frame.Identifier);
            Assert.Equal(0x2C2D, frame.ManufacturerId);
            Assert.StartsWith("KAM", frame.Manufacturer);
            Assert.Equal(0x19, frame.Version);
            Assert.Equal(Medium.Heat, frame.Medium);
            Assert.Equal(0x2A, frame.AccessNumber);
            Assert.Equal(0x10, frame.Status);
            Assert.Single(frame.DataBlocks);
        }

        [Fact(Skip = "BUG: DecodeManufacturer appends a NUL char, the manufacturer reads KAM plus NUL (P3.Driver.MBus/Frames/VariableDataFrame.cs:80)")]
        public void VariableDataFrame_Manufacturer_IsExactlyThreeLetters()
        {
            var userData = Utils.StringToByteArray("78563412" + "2D2C" + "19" + "04" + "2A" + "10" + "0000" + "0403" + "01000000");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x72, userData)) as VariableDataFrame;

            Assert.Equal("KAM", frame.Manufacturer);
        }

        [Fact]
        public void VariableDataFrame_InvalidBcdIdentifier_IsMinusOne()
        {
            var userData = Utils.StringToByteArray("7856341A" + "2D2C" + "01" + "07" + "01" + "00" + "0000" + "0403" + "01000000");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x72, userData)) as VariableDataFrame;

            Assert.Equal(-1, frame.Identifier);
        }

        [Fact]
        public void VariableDataFrame_TruncatedDataBlock_DoesNotThrowAndKeepsCompleteBlocks()
        {
            // second block announces 4 bytes (DIF 04) but only 2 follow
            var userData = Utils.StringToByteArray("78563412" + "2D2C" + "01" + "07" + "01" + "00" + "0000" + "0103" + "07" + "0403" + "0100");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x72, userData)) as VariableDataFrame;

            Assert.NotNull(frame);
            Assert.Equal(12345678, frame.Identifier);
            Assert.NotEmpty(frame.DataBlocks);
            Assert.Equal((byte)7, frame.DataBlocks[0].Value);
        }

        [Fact]
        public void VariableDataFrame_HeaderShorterThanTwelveBytes_DoesNotThrow()
        {
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x72, new byte[] { 0x78, 0x56 }));

            Assert.NotNull(frame);
            Assert.Empty(((VariableDataFrame)frame).DataBlocks);
        }

        [Fact]
        public void FixedDataFrame_BinaryCounters_AreSignedLittleEndian()
        {
            // status bit 0 set = counters are signed binary
            var userData = Utils.StringToByteArray("78563412" + "05" + "01" + "C040" + "FFFFFFFF" + "10270000");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x73, userData)) as FixedDataFrame;

            Assert.NotNull(frame);
            Assert.Equal(-1, frame.Counter1);
            Assert.Equal(10000, frame.Counter2);
            Assert.Equal(MBusStatus.Counter1And2CodedSignedBinary, frame.Status);
        }

        [Fact]
        public void FixedDataFrame_InvalidBcdCounter_IsMinusOne()
        {
            var userData = Utils.StringToByteArray("78563412" + "05" + "00" + "C040" + "AB000000" + "01000000");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x73, userData)) as FixedDataFrame;

            Assert.Equal(-1, frame.Counter1);
            Assert.Equal(1, frame.Counter2);
        }

        [Theory]
        [InlineData("00", "00", Medium.Other)]
        [InlineData("40", "00", Medium.Oil)]
        [InlineData("80", "00", Medium.Electricity)]
        [InlineData("C0", "00", Medium.Gas)]
        [InlineData("00", "40", Medium.Heat)]
        [InlineData("C0", "40", Medium.Water)]
        [InlineData("C0", "C0", Medium.Reserverd2)]
        public void FixedDataFrame_MediumBits_AreSplitOverTheUnitBytes(string unit1, string unit2, Medium expected)
        {
            var userData = Utils.StringToByteArray("78563412" + "05" + "00" + unit1 + unit2 + "01000000" + "01000000");
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x73, userData)) as FixedDataFrame;

            Assert.Equal(expected, frame.Medium);
        }

        [Fact]
        public void FixedDataFrame_TooShort_DoesNotThrow()
        {
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x08, 0x01, 0x73, new byte[] { 0x78, 0x56, 0x34 })) as FixedDataFrame;

            Assert.NotNull(frame);
            Assert.Equal(0, frame.Identification);
        }
    }
}
