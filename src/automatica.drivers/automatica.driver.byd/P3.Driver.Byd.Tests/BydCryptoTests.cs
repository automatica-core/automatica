using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using P3.Driver.Byd.Driver;
using P3.Driver.Byd.Driver.Crypto;
using Xunit;

namespace P3.Driver.Byd.Tests
{
    public class BydCryptoTests
    {
        // expected values computed with Python (hashlib/json), following pyBYD's hashing.py
        [Theory]
        [InlineData("a=1&b=2&password=ABC", "DCb59C51C6eaEC5312edF9823379EE91322eCD55")]
        [InlineData("hello", "AAf4C61dDCc5E8a2DAbeDEf3B482Cd9AEa9434d")]
        public void Sha1Mixed_MatchesPython(string value, string expected)
        {
            Assert.Equal(expected, BydCrypto.Sha1Mixed(value));
        }

        [Fact]
        public void CheckCode_MatchesPython()
        {
            var payload = new List<KeyValuePair<string, string>> { new("b", "2"), new("a", "xä"), new("n", null) };
            Assert.Equal("00cc78fa78a8b1b2018bc753febf73df", BydCrypto.CheckCode(payload));
        }

        [Fact]
        public void LoginKey_IsDoubleUpperMd5()
        {
            Assert.Equal("52E8D430D7BF557F318AC0D094FBA41F", BydCrypto.LoginKey("pw"));
        }

        [Fact]
        public void PythonJson_EnsureAscii()
        {
            var fields = new List<KeyValuePair<string, string>> { new("k", "ä\"/") };
            Assert.Equal("{\"k\":\"\\u00e4\\\"/\"}", BydCrypto.PythonJson(fields, ensureAscii: true));
            Assert.Equal("{\"k\":\"ä\\\"/\"}", BydCrypto.PythonJson(fields, ensureAscii: false));
        }

        [Fact]
        public void SignString_SortsKeysAndWritesNull()
        {
            var fields = new List<KeyValuePair<string, string>> { new("b", "2"), new("a", "1"), new("c", null), new("B", "x") };
            Assert.Equal("B=x&a=1&b=2&c=null&password=PW", BydCrypto.SignString(fields, "PW"));
        }

        [Fact]
        public void Aes_RoundTripAsUpperHex()
        {
            var cipher = BydCrypto.AesEncryptHex("{\"k\":\"v\"}", BydCrypto.LoginKey("pw"));
            Assert.Equal(cipher.ToUpperInvariant(), cipher);
            Assert.Equal(32, cipher.Length);
            Assert.Equal("{\"k\":\"v\"}", BydCrypto.AesDecryptHex(cipher, BydCrypto.LoginKey("pw")));
        }

        [Theory]
        [InlineData("")]
        [InlineData("{\"a\":1}")]
        [InlineData("xxxxxxxxxxxxxxxx")]
        [InlineData("{\"countryCode\":\"DE\",\"identifier\":\"anna@example.com\",\"reqTimestamp\":\"1762520000123\",\"name\":\"Jörg\"}")]
        public void Bangcle_RoundTrip(string text)
        {
            var envelope = Bangcle.Default.Encode(text);

            Assert.StartsWith("F", envelope);
            var cipher = Convert.FromBase64String(envelope[1..]);
            Assert.Equal(0, cipher.Length % 16);
            Assert.True(cipher.Length > Encoding.UTF8.GetByteCount(text));
            Assert.Equal(text, Bangcle.Default.Decode(envelope));
        }

        /// <summary>
        /// Encryption and decryption use separate table sets, a mistake in either port breaks the round trip
        /// </summary>
        [Fact]
        public void Bangcle_BlockRoundTrip_RandomBlocks()
        {
            var random = new Random(42);
            var block = new byte[16];
            var encrypted = new byte[16];
            var decrypted = new byte[16];
            var distinct = new HashSet<string>();
            for (var i = 0; i < 200; i++)
            {
                random.NextBytes(block);
                Bangcle.Default.EncryptBlock(block, encrypted);
                Bangcle.Default.DecryptBlock(encrypted, decrypted);
                Assert.Equal(block, decrypted);
                Assert.NotEqual(block, encrypted);
                distinct.Add(Convert.ToHexString(encrypted));
            }
            Assert.Equal(200, distinct.Count);
        }

        [Fact]
        public void Bangcle_Cbc_ChainsBlocks()
        {
            var cipher = Bangcle.Default.EncryptCbc(new byte[32]);
            // equal plain blocks give different cipher blocks in CBC
            Assert.NotEqual(cipher.AsSpan(0, 16).ToArray(), cipher.AsSpan(16, 16).ToArray());
            Assert.Equal(new byte[32], Bangcle.Default.DecryptCbc(cipher));
        }

        [Fact]
        public void Bangcle_DecodeAcceptsUrlSafeAndWhitespace()
        {
            var envelope = Bangcle.Default.Encode("{\"code\":\"0\",\"x\":\"????>>>>\"}");
            var urlSafe = envelope.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            Assert.Equal("{\"code\":\"0\",\"x\":\"????>>>>\"}", Bangcle.Default.Decode(" " + urlSafe.Insert(5, "\n") + " "));
        }

        [Fact]
        public void Bangcle_InvalidInput_Throws()
        {
            Assert.Throws<InvalidDataException>(() => Bangcle.Default.Decode("Xabc"));
            Assert.Throws<InvalidDataException>(() => Bangcle.Default.Decode("F" + Convert.ToBase64String(new byte[15])));
            Assert.Throws<InvalidDataException>(() => Bangcle.Load(new byte[100]));
        }

        [Theory]
        [InlineData(21, 10, 7, 1)]
        [InlineData(31, 30, 17, 5)]
        [InlineData(15, 20, 1, 3)]
        [InlineData(40, 90, 17, 5)]
        [InlineData(5, 0, 1, 1)]
        public void ClimateParams(double temperature, int minutes, int scale, int timeSpan)
        {
            using var doc = JsonDocument.Parse(BydCloud.ClimateParams(temperature, minutes));
            var root = doc.RootElement;
            Assert.Equal(scale, root.GetProperty("mainSettingTemp").GetInt32());
            Assert.Equal(scale, root.GetProperty("copilotSettingTemp").GetInt32());
            Assert.Equal(timeSpan, root.GetProperty("timeSpan").GetInt32());
            Assert.Equal(4, root.GetProperty("remoteMode").GetInt32());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("airSet").ValueKind);
            // keys sorted like json.dumps(sort_keys=True)
            var keys = root.EnumerateObject().Select(p => p.Name).ToList();
            Assert.Equal(keys.OrderBy(k => k, StringComparer.Ordinal), keys);
        }

        [Theory]
        [InlineData("{\"controlState\":1}", 1)]
        [InlineData("{\"controlState\":\"2\"}", 2)]
        [InlineData("{\"controlState\":0}", 0)]
        [InlineData("{\"res\":1}", 0)]
        [InlineData("{\"res\":2}", 1)]
        [InlineData("{\"res\":3}", 2)]
        [InlineData("{\"requestSerial\":\"x\"}", 0)]
        public void ControlState(string json, int expected)
        {
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(expected, BydCloud.ControlState(doc.RootElement));
        }

        [Theory]
        [InlineData("{\"requestSerial\":\"x\"}", false)]
        [InlineData("{\"onlineState\":2,\"enduranceMileage\":300}", false)]
        [InlineData("{\"onlineState\":1,\"enduranceMileage\":300}", true)]
        [InlineData("{\"onlineState\":1,\"time\":1762520000}", true)]
        [InlineData("{\"onlineState\":1,\"leftFrontTirepressure\":2.4}", true)]
        [InlineData("{\"onlineState\":1,\"enduranceMileage\":0}", false)]
        public void IsRealtimeReady(string json, bool expected)
        {
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(expected, BydCloud.IsRealtimeReady(doc.RootElement));
        }
    }
}
