using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.SpotPrice.Driver;
using Xunit;

namespace P3.Driver.SpotPrice.Tests
{
    public class SpotPriceSourceTests
    {
        [Fact]
        public void Awattar_Parse_TakesTheSlotLengthFromTheData()
        {
            const string json = "{\"object\":\"list\",\"data\":[" +
                                "{\"start_timestamp\":1794117600000,\"end_timestamp\":1794121200000,\"marketprice\":223.71,\"unit\":\"Eur/MWh\"}," +
                                "{\"start_timestamp\":1794121200000,\"end_timestamp\":1794122100000,\"marketprice\":-5.5,\"unit\":\"Eur/MWh\"}]," +
                                "\"url\":\"/at/v1/marketdata\"}";

            var slots = AwattarSource.Parse(json);

            Assert.Equal(2, slots.Count);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1794117600000), slots[0].Start);
            Assert.Equal(TimeSpan.FromHours(1), slots[0].Duration);
            Assert.Equal(223.71, slots[0].Market);
            Assert.Equal(TimeSpan.FromMinutes(15), slots[1].Duration);
            Assert.Equal(-5.5, slots[1].Market);
        }

        [Fact]
        public void Awattar_Parse_WithoutData_Throws()
        {
            Assert.Throws<SpotPriceException>(() => AwattarSource.Parse("{\"error\":\"x\"}"));
        }

        internal const string EntsoeXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Publication_MarketDocument xmlns="urn:iec62325.351:tc57wg16:451-3:publicationdocument:7:3">
              <mRID>1</mRID>
              <type>A44</type>
              <TimeSeries>
                <mRID>1</mRID>
                <curveType>A03</curveType>
                <Period>
                  <timeInterval><start>2026-11-07T23:00Z</start><end>2026-11-08T00:00Z</end></timeInterval>
                  <resolution>PT15M</resolution>
                  <Point><position>1</position><price.amount>100.5</price.amount></Point>
                  <Point><position>2</position><price.amount>90</price.amount></Point>
                  <Point><position>4</position><price.amount>-1.25</price.amount></Point>
                </Period>
              </TimeSeries>
              <TimeSeries>
                <mRID>2</mRID>
                <curveType>A03</curveType>
                <Period>
                  <timeInterval><start>2026-11-07T23:00Z</start><end>2026-11-08T01:00Z</end></timeInterval>
                  <resolution>PT60M</resolution>
                  <Point><position>1</position><price.amount>999</price.amount></Point>
                  <Point><position>2</position><price.amount>80</price.amount></Point>
                </Period>
              </TimeSeries>
            </Publication_MarketDocument>
            """;

        [Fact]
        public void Entsoe_Parse_RepeatsOmittedPoints_AndPrefersQuarterHours()
        {
            var schedule = new SpotPriceSchedule(EntsoeSource.Parse(EntsoeXml));

            var start = new DateTimeOffset(2026, 11, 7, 23, 0, 0, TimeSpan.Zero);
            Assert.Equal(new[] { 100.5, 90, 90, -1.25, 80 }, schedule.Slots.Select(s => s.Market));
            Assert.Equal(start.AddMinutes(30), schedule.Slots[2].Start);
            Assert.Equal(start.AddHours(1), schedule.Slots[4].Start);
            Assert.Equal(TimeSpan.FromHours(1), schedule.Slots[4].Duration);
        }

        [Fact]
        public void Entsoe_NoMatchingData_IsEmpty()
        {
            const string xml = "<Acknowledgement_MarketDocument xmlns=\"urn:iec62325.351:tc57wg16:451-1:acknowledgementdocument:7:0\">" +
                               "<Reason><code>999</code><text>No matching data found for Data item Day-ahead Prices</text></Reason></Acknowledgement_MarketDocument>";
            Assert.Empty(EntsoeSource.Parse(xml));
        }

        [Fact]
        public void Entsoe_OtherAcknowledgement_ThrowsWithTheReason()
        {
            const string xml = "<Acknowledgement_MarketDocument xmlns=\"urn:iec62325.351:tc57wg16:451-1:acknowledgementdocument:7:0\">" +
                               "<Reason><code>999</code><text>Invalid query attributes</text></Reason></Acknowledgement_MarketDocument>";
            var ex = Assert.Throws<SpotPriceException>(() => EntsoeSource.Parse(xml));
            Assert.Contains("Invalid query attributes", ex.Message);
        }

        [Fact]
        public async Task Entsoe_Unauthorized_TheMessageDoesNotContainTheToken()
        {
            await using var server = await FakeHttpServer.StartAsync();
            server.Handler = _ => (401, "<Acknowledgement_MarketDocument xmlns=\"urn:x\"><Reason><code>999</code><text>Authentication failed.</text></Reason></Acknowledgement_MarketDocument>");
            using var http = new HttpClient();
            var source = new EntsoeSource(http, server.Url + "/api", "secret-token-123", "");

            var ex = await Assert.ThrowsAsync<SpotPriceException>(() => source.FetchAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), CancellationToken.None));

            Assert.Contains("Authentication failed.", ex.Message);
            Assert.DoesNotContain("secret-token-123", ex.Message);
        }

        [Fact]
        public async Task Entsoe_WithoutToken_DoesNotCall()
        {
            await using var server = await FakeHttpServer.StartAsync();
            using var http = new HttpClient();
            var source = new EntsoeSource(http, server.Url + "/api", "", "");

            await Assert.ThrowsAsync<SpotPriceException>(() => source.FetchAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), CancellationToken.None));
            Assert.Empty(server.Requests);
        }
    }
}
