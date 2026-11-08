using System;
using System.IO;
using System.Linq;
using System.Text;
using Automatica.Core.Base.Media;
using Xunit;

namespace Automatica.Core.Tests.Media
{
    public class RadioStationCatalogTests
    {
        [Fact]
        public void BuiltIn_HasAustrianAndGermanStations_WithValidKeysAndStreams()
        {
            var stations = RadioStationCatalog.BuiltIn.Stations;

            Assert.True(stations.Count >= 40, $"only {stations.Count} built-in stations");
            Assert.Contains(stations, a => a.Country == "AT");
            Assert.Contains(stations, a => a.Country == "DE");
            Assert.All(stations, a =>
            {
                Assert.True(RadioStationCatalog.IsValidKey(a.Key), a.Key);
                Assert.True(RadioStationCatalog.IsValidStreamUrl(a.StreamUrl), a.StreamUrl);
                Assert.False(string.IsNullOrWhiteSpace(a.Name), a.Key);
                Assert.False(a.IsCustom);
                if (a.Logo != null)
                {
                    Assert.True(RadioStationCatalog.IsValidStreamUrl(a.Logo), a.Logo);
                }
            });
            Assert.Equal(stations.Count, stations.Select(a => a.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void BuiltIn_IsSortedByCountryAndName()
        {
            var stations = RadioStationCatalog.BuiltIn.Stations;
            var sorted = stations.OrderBy(a => a.Country, StringComparer.OrdinalIgnoreCase).ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

            Assert.Equal(sorted.Select(a => a.Key), stations.Select(a => a.Key));
        }

        [Theory]
        [InlineData("at-oe3", "at-oe3")]
        [InlineData("AT-OE3", "at-oe3")]
        [InlineData("fm4", "at-fm4")]
        [InlineData(" FM4 ", "at-fm4")]
        public void Find_ByKeyOrName(string value, string key)
        {
            Assert.Equal(key, RadioStationCatalog.BuiltIn.Find(value)?.Key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("no-such-station")]
        public void Find_Unknown_IsNull(string value)
        {
            Assert.Null(RadioStationCatalog.BuiltIn.Find(value));
        }

        [Fact]
        public void LaterStation_ReplacesTheOneWithTheSameKey()
        {
            var catalog = new RadioStationCatalog(new[]
            {
                new RadioStation { Key = "a", Name = "Built-in", StreamUrl = "http://a.example/1" },
                new RadioStation { Key = "A", Name = "Mine", StreamUrl = "http://a.example/2", IsCustom = true },
                new RadioStation { Key = " ", Name = "no key", StreamUrl = "http://a.example/3" }
            });

            var station = Assert.Single(catalog.Stations);
            Assert.Equal("Mine", station.Name);
            Assert.True(catalog.Find("a").IsCustom);
        }

        [Fact]
        public void Resolve_CatalogStation()
        {
            var station = RadioStationCatalog.BuiltIn.Resolve("at-oe3");

            Assert.Equal("at-oe3", station.Key);
            Assert.StartsWith("http", station.StreamUrl);
        }

        [Theory]
        [InlineData("s8007", "s8007")]
        [InlineData("8007", "s8007")]
        [InlineData("S8007", "s8007")]
        public void Resolve_TuneInId(string value, string tuneInId)
        {
            var station = RadioStationCatalog.BuiltIn.Resolve(value);

            Assert.Equal(tuneInId, station.TuneInId);
            Assert.Equal("", station.StreamUrl);
        }

        [Theory]
        [InlineData("http://stream.example/live.mp3")]
        [InlineData("x-rincon-mp3radio://stream.example/live")]
        [InlineData("x-sonosapi-stream:s24896?sid=254&flags=8224")]
        public void Resolve_UrlIsKeptAsStream(string url)
        {
            var station = RadioStationCatalog.BuiltIn.Resolve(url);

            Assert.Equal(url, station.StreamUrl);
            Assert.Null(station.TuneInId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("not a station")]
        public void Resolve_Nothing_IsNull(string value)
        {
            Assert.Null(RadioStationCatalog.BuiltIn.Resolve(value));
        }

        [Theory]
        [InlineData("at-oe3", true)]
        [InlineData("1live", true)]
        [InlineData("a", true)]
        [InlineData("At-oe3", false)]
        [InlineData("-oe3", false)]
        [InlineData("oe 3", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidKey(string key, bool valid)
        {
            Assert.Equal(valid, RadioStationCatalog.IsValidKey(key));
            Assert.False(RadioStationCatalog.IsValidKey(new string('a', 65)));
        }

        [Theory]
        [InlineData("http://a.example/live", true)]
        [InlineData("https://a.example", true)]
        [InlineData("ftp://a.example/live", false)]
        [InlineData("x-rincon-mp3radio://a.example/live", false)]
        [InlineData("a.example/live", false)]
        [InlineData(null, false)]
        public void IsValidStreamUrl(string url, bool valid)
        {
            Assert.Equal(valid, RadioStationCatalog.IsValidStreamUrl(url));
        }

        [Fact]
        public void ParseKeys_AndJoinKeys()
        {
            Assert.Equal(new[] { "at-oe3", "de-1live", "Ö3=s8007" }, RadioStationCatalog.ParseKeys(" at-oe3 ;de-1live,\nAT-OE3;;Ö3=s8007\r\n"));
            Assert.Empty(RadioStationCatalog.ParseKeys(null));
            Assert.Equal("a;b", RadioStationCatalog.JoinKeys(new[] { "a", "b" }));
        }

        [Fact]
        public void Parse_SkipsInvalidStations()
        {
            var json = "{\"stations\":[{\"key\":\"ok\",\"name\":\"Ok\",\"streamUrl\":\"https://a.example/x\",\"isCustom\":true}," +
                       "{\"key\":\"Bad Key\",\"name\":\"x\",\"streamUrl\":\"https://a.example/x\"}," +
                       "{\"key\":\"no-url\",\"name\":\"x\",\"streamUrl\":\"\"}]}";

            var stations = RadioStationCatalog.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json)));

            var station = Assert.Single(stations);
            Assert.Equal("ok", station.Key);
            Assert.False(station.IsCustom);
        }
    }
}
