using System;
using System.Collections.Generic;
using System.Linq;
using P3.Driver.Knx.DriverFactory.Dpt;
using Xunit;

namespace P3.Driver.Knx.Tests.Dpt
{
    /// <summary>
    /// Round trip properties over every catalogue datapoint type and sub type: decoding a payload, encoding the decoded values
    /// and decoding again gives the same values, and the encoded payload has the size of the datapoint type.
    /// </summary>
    public class KnxDptCodecPropertyTests
    {
        public static IEnumerable<object[]> CatalogTypes()
        {
            foreach (var entry in KnxDptCatalog.Entries)
            {
                foreach (var subType in entry.SubTypes.DefaultIfEmpty(entry.DefaultSubType).Distinct())
                {
                    yield return new object[] { entry.Key, subType };
                }
            }
        }

        private static IEnumerable<byte[]> Payloads(IKnxDptCodec codec)
        {
            var length = codec.SizeInBit > 0 ? Math.Max(1, (codec.SizeInBit + 7) / 8) : 14;
            var mask = codec.SizeInBit is > 0 and < 8 ? (byte)((1 << codec.SizeInBit) - 1) : (byte)0xFF;

            foreach (var pattern in new byte[] { 0x00, 0x01, 0x10, 0x2A, 0x55, 0x7F, 0x80, 0xAA, 0xC3, 0xFE, 0xFF })
            {
                var data = new byte[length];
                for (var i = 0; i < length; i++)
                {
                    data[i] = (byte)((pattern + i * 0x11) & mask);
                }
                yield return data;
            }
        }

        private static void AssertSameValues(IReadOnlyDictionary<string, object> expected, IReadOnlyDictionary<string, object> actual, string context)
        {
            Assert.True(expected.Keys.OrderBy(a => a).SequenceEqual(actual.Keys.OrderBy(a => a)),
                $"{context}: keys {string.Join(",", expected.Keys)} != {string.Join(",", actual.Keys)}");

            foreach (var (key, value) in expected)
            {
                var other = actual[key];
                if (value is double or float)
                {
                    var a = Convert.ToDouble(value);
                    var b = Convert.ToDouble(other);
                    Assert.True((double.IsNaN(a) && double.IsNaN(b)) || Math.Abs(a - b) <= Math.Max(1e-6, Math.Abs(a) * 1e-6), $"{context}: {key} {a} != {b}");
                }
                else
                {
                    Assert.True(Equals(value, other) || Equals(value?.ToString(), other?.ToString()), $"{context}: {key} '{value}' != '{other}'");
                }
            }
        }

        [Theory]
        [MemberData(nameof(CatalogTypes))]
        public void DecodeEncodeDecode_IsStable(string key, int subType)
        {
            var codec = KnxDptCatalog.GetByKey(key).CreateCodec(subType);
            var checkedPayloads = 0;

            foreach (var payload in Payloads(codec))
            {
                IReadOnlyDictionary<string, object> decoded;
                try
                {
                    decoded = codec.Decode(payload);
                }
                catch (FormatException)
                {
                    continue; // not a valid telegram for this type
                }

                if (decoded.Count == 0)
                {
                    continue; // all fields flagged invalid
                }

                var context = $"{key}.{subType:000} {Convert.ToHexString(payload)}";
                var encoded = codec.Encode(decoded, decoded.Keys.First());
                if (codec.SizeInBit > 0)
                {
                    Assert.Equal(Math.Max(1, (codec.SizeInBit + 7) / 8), encoded.Length);
                }

                AssertSameValues(decoded, codec.Decode(encoded), context);
                Assert.Equal(encoded, codec.Encode(codec.Decode(encoded), decoded.Keys.First())); // encoding is a fixed point
                checkedPayloads++;
            }

            Assert.True(checkedPayloads > 0, $"{key}.{subType:000}: no payload decoded");
        }

        [Theory]
        [MemberData(nameof(CatalogTypes))]
        public void Decode_WrongLength_ThrowsFormatException(string key, int subType)
        {
            var codec = KnxDptCatalog.GetByKey(key).CreateCodec(subType);
            if (codec.SizeInBit == 0)
            {
                return; // variable length strings accept any length
            }

            var length = Math.Max(1, (codec.SizeInBit + 7) / 8);

            Assert.Throws<FormatException>(() => codec.Decode(new byte[length + 1]));
            Assert.Throws<FormatException>(() => codec.Decode(null));
            if (length > 1)
            {
                Assert.Throws<FormatException>(() => codec.Decode(new byte[length - 1]));
            }
        }

        [Theory]
        [MemberData(nameof(CatalogTypes))]
        public void Encode_NoValues_GivesAPayloadOfTheTypeSize(string key, int subType)
        {
            var codec = KnxDptCatalog.GetByKey(key).CreateCodec(subType);

            var data = codec.Encode(new Dictionary<string, object>(), codec.Values.First().Key);

            if (codec.SizeInBit > 0)
            {
                Assert.Equal(Math.Max(1, (codec.SizeInBit + 7) / 8), data.Length);
            }
            else
            {
                Assert.NotNull(data);
            }
        }
    }
}
