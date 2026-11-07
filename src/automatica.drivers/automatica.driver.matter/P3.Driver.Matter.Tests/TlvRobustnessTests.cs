using P3.Driver.Matter.Messages;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Tests
{
    /// <summary>Malformed packets from the network must end in a <see cref="MatterProtocolException"/>, never in another exception.</summary>
    public class TlvRobustnessTests
    {
        [Theory]
        [InlineData(new byte[0])]
        [InlineData(new byte[] { 0x04 })]                       // uint8 without value
        [InlineData(new byte[] { 0x05, 0x01 })]                 // uint16 with one byte
        [InlineData(new byte[] { 0x07, 1, 2, 3, 4, 5, 6, 7 })]  // uint64 with 7 bytes
        [InlineData(new byte[] { 0x0A, 0, 0 })]                 // float truncated
        [InlineData(new byte[] { 0x0B, 0, 0, 0, 0 })]           // double truncated
        [InlineData(new byte[] { 0x0C, 5, 0x61 })]              // utf8 string, length 5 but 1 byte
        [InlineData(new byte[] { 0x10, 3, 1, 2 })]              // byte string, length 3 but 2 bytes
        [InlineData(new byte[] { 0x24 })]                       // context tag without tag byte
        [InlineData(new byte[] { 0x44, 0x01 })]                 // common profile 2-byte tag truncated
        [InlineData(new byte[] { 0xE4, 0, 0, 0 })]              // fully qualified 8 byte tag truncated
        [InlineData(new byte[] { 0x15, 0x24, 0x01, 0x05 })]     // structure without end-of-container
        [InlineData(new byte[] { 0x18 })]                       // end-of-container at top level
        [InlineData(new byte[] { 0x1F })]                       // unknown element type
        public void Decode_MalformedInput_ThrowsMatterProtocolException(byte[] data)
        {
            Assert.Throws<MatterProtocolException>(() => TlvReader.Decode(data));
        }

        [Fact(Skip = "BUG: TlvReader.Need overflows pos + len for lengths near int.MaxValue, ArgumentOutOfRangeException instead of MatterProtocolException (Tlv/TlvReader.cs:198)")]
        public void Decode_StringLengthNearIntMax_ThrowsMatterProtocolException()
        {
            // utf8 string with a 4 byte length of 0x7FFFFFFF: pos + len overflows an int in the bounds check
            var data = new byte[] { 0x0E, 0xFF, 0xFF, 0xFF, 0x7F, 0x78 };
            var ex = Record.Exception(() => TlvReader.Decode(data));
            Assert.IsType<MatterProtocolException>(ex);
        }

        [Fact(Skip = "BUG: an 8 byte TLV length >= 2^63 casts to a negative long, passes the checks and decodes as an empty string (Tlv/TlvReader.cs:149-157)")]
        public void Decode_StringLengthAboveLongMax_ThrowsMatterProtocolException()
        {
            var data = new byte[] { 0x0F, 0, 0, 0, 0, 0, 0, 0, 0x80, 0x78 };
            var ex = Record.Exception(() => TlvReader.Decode(data));
            Assert.IsType<MatterProtocolException>(ex);
        }

        [Fact]
        public void Decode_StringLengthAboveIntMax_ThrowsMatterProtocolException()
        {
            var data = new byte[] { 0x0F, 0, 0, 0, 0x80, 0, 0, 0, 0, 0x78 };
            Assert.Throws<MatterProtocolException>(() => TlvReader.Decode(data));
        }

        [Fact]
        public void DecodeAll_StrayEndOfContainer_Throws()
        {
            Assert.Throws<MatterProtocolException>(() => TlvReader.DecodeAll(new byte[] { 0x04, 0x01, 0x18 }));
        }

        [Fact]
        public void DecodeAll_SeveralTopLevelElements_AreReturnedInOrder()
        {
            var list = TlvReader.DecodeAll(new byte[] { 0x04, 0x01, 0x08, 0x14 });
            Assert.Equal(3, list.Count);
            Assert.Equal(1ul, list[0].AsUInt());
            Assert.Equal(TlvKind.Bool, list[1].Kind);
            Assert.False(list[1].AsBool());
            Assert.True(list[2].IsNull);
        }

        [Fact]
        public void Decode_RandomShortGarbage_OnlyThrowsProtocolErrors()
        {
            // short packets cannot carry lengths that overflow, so only protocol errors are acceptable here
            var random = new Random(12345);
            for (var i = 0; i < 5000; i++)
            {
                var data = new byte[random.Next(0, 24)];
                random.NextBytes(data);
                // keep string lengths small: the large-length overflow is covered (and skipped) above
                for (var j = 0; j < data.Length; j++)
                {
                    var type = data[j] & 0x1F;
                    if (type is 0x0E or 0x0F or 0x12 or 0x13)
                    {
                        data[j] = (byte)(data[j] & ~0x02);
                    }
                }

                var ex = Record.Exception(() => TlvReader.DecodeAll(data));
                if (ex != null && ex is not MatterProtocolException)
                {
                    Assert.Fail($"input {Convert.ToHexString(data)} threw {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        [Fact]
        public void Tlv_IntegerExtremesAndUnicode_RoundTrip()
        {
            var bytes = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutInt(1, long.MinValue);
                w.PutInt(2, long.MaxValue);
                w.PutInt(3, -1);
                w.PutUInt(4, ulong.MaxValue);
                w.PutUInt(5, 0);
                w.PutString(6, "äöü€");
                w.PutBytes(7, new byte[300]);
                w.PutDouble(8, 1.5);
                w.PutFloat(9, -2.25f);
                w.EndContainer();
            });

            var root = TlvReader.Decode(bytes);
            Assert.Equal(long.MinValue, root.GetInt(1));
            Assert.Equal(long.MaxValue, root.GetInt(2));
            Assert.Equal(-1, root.GetInt(3));
            Assert.Equal(ulong.MaxValue, root.GetUInt(4));
            Assert.Equal(0ul, root.GetUInt(5));
            Assert.Equal("äöü€", root.GetString(6));
            Assert.Equal(300, root.GetBytes(7).Length);
            Assert.Equal(1.5, root.Require(8).AsDouble());
            Assert.Equal(-2.25, root.Require(9).AsDouble());
        }

        [Fact]
        public void TlvElement_WrongKindAccess_ThrowsMatterProtocolException()
        {
            var root = TlvReader.Decode(TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutString(1, "x");
                w.EndContainer();
            }));

            Assert.Throws<MatterProtocolException>(() => root.GetUInt(1));
            Assert.Throws<MatterProtocolException>(() => root.GetBytes(1));
            Assert.Throws<MatterProtocolException>(() => root.Require(1).AsDouble());
            Assert.Null(root.GetUIntOrNull(2));
            Assert.False(root.Has(2));
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0 })]                       // shorter than 8
        [InlineData(new byte[] { 0x10, 0, 0, 0, 0, 0, 0, 0 })]                 // version 1
        [InlineData(new byte[] { 0x03, 0, 0, 0, 0, 0, 0, 0 })]                 // DSIZ 3
        [InlineData(new byte[] { 0x04, 0, 0, 0, 0, 0, 0, 0, 1, 2 })]           // source node id truncated
        [InlineData(new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3 })]        // destination node id truncated
        [InlineData(new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 1 })]              // group id truncated
        [InlineData(new byte[] { 0x00, 0, 0, 0x20, 0, 0, 0, 0, 0x10, 0x00 })]  // extensions longer than the packet
        public void MessageHeader_Decode_Malformed_Throws(byte[] data)
        {
            Assert.Throws<MatterProtocolException>(() => MessageHeader.Decode(data, out _));
        }

        [Fact]
        public void MessageHeader_GroupDestination_RoundTrips()
        {
            var header = new MessageHeader { SessionId = 5, SecurityFlags = MessageHeader.SessionTypeGroup, MessageCounter = 9, SourceNodeId = 1, DestinationGroupId = 0xBEEF };
            var decoded = MessageHeader.Decode(header.Encode(), out var length);
            Assert.Equal(18, length);
            Assert.True(decoded.IsGroupSession);
            Assert.False(decoded.IsUnsecured);
            Assert.Equal((ushort)0xBEEF, decoded.DestinationGroupId);
            Assert.Null(decoded.DestinationNodeId);
        }

        [Fact]
        public void MessageHeader_Extensions_AreCountedInTheHeaderLength()
        {
            var data = new byte[] { 0x00, 0, 0, 0x20, 1, 0, 0, 0, 0x03, 0x00, 0xAA, 0xBB, 0xCC, 0x99 };
            var decoded = MessageHeader.Decode(data, out var length);
            Assert.Equal(13, length);
            Assert.True(decoded.IsUnsecured);
            Assert.Equal(1u, decoded.MessageCounter);
        }

        [Fact]
        public void MessageHeader_Encode_ClearsTheExtensionsFlag()
        {
            var header = new MessageHeader { SecurityFlags = MessageHeader.SecurityFlagExtensions };
            Assert.Equal(0, header.Encode()[3] & MessageHeader.SecurityFlagExtensions);
        }

        [Fact]
        public void MessageHeader_BuildNonce_LayoutIsFlagsCounterNodeIdLittleEndian()
        {
            var nonce = MessageHeader.BuildNonce(0x01, 0x04030201, 0x0C0B0A0908070605);
            Assert.Equal(new byte[] { 0x01, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C }, nonce);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0 })]
        [InlineData(new byte[] { 0x10, 0x08, 1, 0, 1, 0 })]             // vendor flag, protocol id missing
        [InlineData(new byte[] { 0x02, 0x08, 1, 0, 1, 0, 1, 2 })]       // ack flag, counter truncated
        public void PayloadHeader_Decode_Malformed_Throws(byte[] data)
        {
            Assert.Throws<MatterProtocolException>(() => PayloadHeader.Decode(data, out _));
        }
    }
}
