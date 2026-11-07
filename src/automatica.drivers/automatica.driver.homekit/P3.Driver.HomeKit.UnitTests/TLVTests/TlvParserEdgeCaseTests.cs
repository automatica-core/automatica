using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Hap.TlvData;
using P3.Driver.HomeKit.Hap.TlvData.Exceptions;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.TLVTests
{
    public class TlvParserEdgeCaseTests
    {
        private readonly TlvParser _parser = new TlvParser(NullLogger.Instance);

        private static byte[] Bytes(int length, byte seed = 1) => Enumerable.Range(0, length).Select(a => (byte)(a + seed)).ToArray();

        [Fact]
        public void Parse_Null_ReturnsAnEmptyTlv()
        {
            Assert.Empty(_parser.Parse(null).Values);
        }

        [Fact]
        public void Parse_Empty_ReturnsAnEmptyTlv()
        {
            Assert.Empty(_parser.Parse(Array.Empty<byte>()).Values);
        }

        [Fact]
        public void Parse_ZeroLengthItem_HasAnEmptyValue()
        {
            var tlv = _parser.Parse(new byte[] { (byte)Constants.Separator, 0, (byte)Constants.State, 1, 4 });

            Assert.Empty(tlv.GetType(Constants.Separator));
            Assert.Equal(4, tlv.GetTypeAsInt(Constants.State));
        }

        [Fact]
        public void Parse_FragmentedItem_IsMerged()
        {
            var value = Bytes(300);
            var data = new byte[] { 3, 255 }.Concat(value.Take(255)).Concat(new byte[] { 3, 45 }).Concat(value.Skip(255)).ToArray();

            Assert.Equal(value, _parser.Parse(data).GetType(Constants.PublicKey));
        }

        [Fact]
        public void Parse_ThreeFragments_AreMerged()
        {
            var value = Bytes(600);
            var tlv = new Tlv();
            tlv.AddType(Constants.PublicKey, value);

            Assert.Equal(value, _parser.Parse(_parser.Serialize(tlv)).GetType(Constants.PublicKey));
        }

        [Fact]
        public void Parse_DuplicateShortItem_KeepsTheFirstValue()
        {
            var tlv = _parser.Parse(new byte[] { 6, 1, 1, 6, 1, 3 });

            Assert.Equal(1, tlv.GetTypeAsInt(Constants.State));
        }

        [Fact]
        public void Parse_DuplicateEmptyItem_IsAFragmentError()
        {
            Assert.Throws<TlvFragmentLengthException>(() => _parser.Parse(new byte[] { 255, 0, 255, 0 }));
        }

        [Fact]
        public void Parse_FragmentAfterAShortLastFragment_IsAFragmentError()
        {
            // 255 + 10 => complete item, another fragment of the same type is invalid
            var data = new byte[] { 3, 255 }.Concat(Bytes(255)).Concat(new byte[] { 3, 10 }).Concat(Bytes(10)).Concat(new byte[] { 3, 1, 9 }).ToArray();

            Assert.Throws<TlvFragmentLengthException>(() => _parser.Parse(data));
        }

        [Fact]
        public void Parse_ItemWithoutLengthByte_Throws()
        {
            Assert.ThrowsAny<Exception>(() => _parser.Parse(new byte[] { 6, 1, 1, 7 }));
        }

        [Fact]
        public void Parse_TruncatedValue_ReturnsTheAvailableBytes()
        {
            var tlv = _parser.Parse(new byte[] { 3, 10, 1, 2, 3 });

            Assert.Equal(new byte[] { 1, 2, 3 }, tlv.GetType(Constants.PublicKey));
        }

        [Fact]
        public void Serialize_EmptyValue_WritesTypeAndZeroLength()
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.Separator, Array.Empty<byte>());

            Assert.Equal(new byte[] { 255, 0 }, _parser.Serialize(tlv));
        }

        [Theory]
        [InlineData(255, new[] { 255 })]
        [InlineData(256, new[] { 255, 1 })]
        [InlineData(510, new[] { 255, 255 })]
        [InlineData(511, new[] { 255, 255, 1 })]
        public void Serialize_LongValues_AreSplitInto255ByteFragments(int length, int[] fragments)
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.EncryptedData, Bytes(length));

            var data = _parser.Serialize(tlv);

            var lengths = new List<int>();
            for (var i = 0; i < data.Length; i += 2 + data[i + 1])
            {
                Assert.Equal((byte)Constants.EncryptedData, data[i]);
                lengths.Add(data[i + 1]);
            }
            Assert.Equal(fragments, lengths);
        }

        [Fact]
        public void Serialize_ThenParse_RoundTripsSeveralItems()
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 3);
            tlv.AddType(Constants.PublicKey, Bytes(384));
            tlv.AddType(Constants.Proof, Bytes(64, 9));

            var parsed = _parser.Parse(_parser.Serialize(tlv));

            Assert.Equal(3, parsed.GetTypeAsInt(Constants.State));
            Assert.Equal(Bytes(384), parsed.GetType(Constants.PublicKey));
            Assert.Equal(Bytes(64, 9), parsed.GetType(Constants.Proof));
        }

        [Fact]
        public void GetTypeAsInt_Missing_IsMinus1()
        {
            Assert.Equal(-1, new Tlv().GetTypeAsInt(Constants.State));
        }

        [Fact]
        public void GetType_Missing_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => new Tlv().GetType(Constants.State));
        }

        [Fact]
        public void HasType_ReflectsAddedItems()
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.Error, ErrorCodes.Busy);

            Assert.True(tlv.HasType(Constants.Error));
            Assert.False(tlv.HasType(Constants.State));
            Assert.Equal((int)ErrorCodes.Busy, tlv.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void Slices_PadsTheLastSliceOnlyWhenAsked()
        {
            var source = Bytes(5);

            Assert.Equal(new[] { 2, 2, 1 }, source.Slices(2).Select(a => a.Length));
            Assert.Equal(new[] { 2, 2, 2 }, source.Slices(2, true).Select(a => a.Length));
            Assert.Equal(source, source.Slices(2).SelectMany(a => a));
        }
    }
}
