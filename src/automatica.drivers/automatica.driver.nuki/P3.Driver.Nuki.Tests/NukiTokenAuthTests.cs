using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.Nuki.Driver;
using Xunit;

namespace P3.Driver.Nuki.Tests
{
    public class NukiTokenAuthTests
    {
        private static readonly DateTime FixedUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        internal static Dictionary<string, string> ParseQuery(string query)
        {
            return QueryHelpers.ParseQuery("?" + query).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        }

        internal static string Sha256Hex(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        }

        [Fact]
        public void CreateHashedQuery_KnownVector_MatchesNukiSpec()
        {
            // sha256("2026-01-02T03:04:05Z,1234,abc123"), computed independently
            var query = NukiTokenAuth.CreateHashedQuery("abc123", FixedUtc, 1234);

            Assert.Equal("ts=2026-01-02T03%3A04%3A05Z&rnr=1234&hash=f17be43ef639c54a6ba1cd2cac301d5544d27cd63871cc07cd52d4af5fd4aaa0", query);
        }

        [Fact]
        public void CreateHashedQuery_SecondKnownVector_Matches()
        {
            var query = NukiTokenAuth.CreateHashedQuery("secret-token", new DateTime(2030, 7, 15, 23, 59, 59, DateTimeKind.Utc), 0);

            Assert.Equal("1313ffa5c95c9d02db8ac62454fc79bfd098c5140ac2b2f463b9d7bea6cd881f", ParseQuery(query)["hash"]);
            Assert.Equal("0", ParseQuery(query)["rnr"]);
        }

        [Fact]
        public void CreateHashedQuery_NeverContainsTheToken()
        {
            var query = NukiTokenAuth.CreateHashedQuery("my-secret-token", FixedUtc, 42);

            Assert.DoesNotContain("my-secret-token", query);
            Assert.DoesNotContain("token=", query);
        }

        [Fact]
        public void CreateHashedQuery_HasExactlyTsRnrAndHash()
        {
            var query = ParseQuery(NukiTokenAuth.CreateHashedQuery("t", FixedUtc, 42));

            Assert.Equal(new[] { "hash", "rnr", "ts" }, query.Keys.OrderBy(k => k, StringComparer.Ordinal));
            Assert.Equal("2026-01-02T03:04:05Z", query["ts"]);
            Assert.Matches("^[0-9a-f]{64}$", query["hash"]);
        }

        [Fact]
        public void CreateHashedQuery_HashIsSha256OfTsCommaRnrCommaToken()
        {
            var query = ParseQuery(NukiTokenAuth.CreateHashedQuery("tok", FixedUtc, 65535));

            Assert.Equal(Sha256Hex("2026-01-02T03:04:05Z,65535,tok"), query["hash"]);
        }

        [Fact]
        public void CreateHashedQuery_DropsFractionalSeconds()
        {
            var query = ParseQuery(NukiTokenAuth.CreateHashedQuery("t", FixedUtc.AddMilliseconds(987), 1));

            Assert.Equal("2026-01-02T03:04:05Z", query["ts"]);
        }

        [Fact]
        public void CreateHashedQuery_LocalTime_IsConvertedToUtc()
        {
            var fromLocal = NukiTokenAuth.CreateHashedQuery("t", FixedUtc.ToLocalTime(), 7);
            var fromUtc = NukiTokenAuth.CreateHashedQuery("t", FixedUtc, 7);

            Assert.Equal(fromUtc, fromLocal);
        }

        [Theory]
        [InlineData("ar-SA")]
        [InlineData("th-TH")]
        [InlineData("de-AT")]
        public void CreateHashedQuery_IndependentOfCurrentCulture(string culture)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);

                var query = NukiTokenAuth.CreateHashedQuery("abc123", FixedUtc, 1234);

                Assert.Equal("ts=2026-01-02T03%3A04%3A05Z&rnr=1234&hash=f17be43ef639c54a6ba1cd2cac301d5544d27cd63871cc07cd52d4af5fd4aaa0", query);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void CreateHashedQuery_DifferentRandomNumber_DifferentHash()
        {
            var a = ParseQuery(NukiTokenAuth.CreateHashedQuery("t", FixedUtc, 1))["hash"];
            var b = ParseQuery(NukiTokenAuth.CreateHashedQuery("t", FixedUtc, 2))["hash"];

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void CreateHashedQuery_DifferentToken_DifferentHash()
        {
            var a = ParseQuery(NukiTokenAuth.CreateHashedQuery("t1", FixedUtc, 1))["hash"];
            var b = ParseQuery(NukiTokenAuth.CreateHashedQuery("t2", FixedUtc, 1))["hash"];

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void CreateHashedQuery_NullToken_HashesEmptyToken()
        {
            var query = ParseQuery(NukiTokenAuth.CreateHashedQuery(null, FixedUtc, 5));

            Assert.Equal(Sha256Hex("2026-01-02T03:04:05Z,5,"), query["hash"]);
        }

        [Fact]
        public void CreateQuery_Hashed_HasValidStructureAndConsistentHash()
        {
            var query = ParseQuery(NukiTokenAuth.CreateQuery("abc123", true));

            Assert.True(DateTime.TryParseExact(query["ts"], "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out _));
            var rnr = int.Parse(query["rnr"], CultureInfo.InvariantCulture);
            Assert.InRange(rnr, 0, 65535);
            Assert.Equal(Sha256Hex($"{query["ts"]},{query["rnr"]},abc123"), query["hash"]);
            Assert.False(query.ContainsKey("token"));
        }

        [Fact]
        public void CreateQuery_Hashed_UsesFreshRandomNumbers()
        {
            var numbers = Enumerable.Range(0, 20).Select(_ => ParseQuery(NukiTokenAuth.CreateQuery("t", true))["rnr"]).Distinct().Count();

            Assert.True(numbers > 1);
        }

        [Fact]
        public void CreateQuery_Plain_SendsTheTokenParameter()
        {
            Assert.Equal("token=abc123", NukiTokenAuth.CreateQuery("abc123", false));
        }

        [Fact]
        public void CreateQuery_Plain_EscapesTheToken()
        {
            var query = NukiTokenAuth.CreateQuery("a b&c=d", false);

            Assert.Equal("token=a%20b%26c%3Dd", query);
            Assert.Equal("a b&c=d", ParseQuery(query)["token"]);
        }

        [Fact]
        public void CreateQuery_PlainNullToken_SendsEmptyToken()
        {
            Assert.Equal("token=", NukiTokenAuth.CreateQuery(null, false));
        }
    }
}
