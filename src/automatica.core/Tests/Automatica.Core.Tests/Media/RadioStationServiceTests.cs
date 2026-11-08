using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Media;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Media;
using Automatica.Core.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Core.Tests.Media
{
    public sealed class RadioStationServiceTests : IDisposable
    {
        private readonly SqliteTestDatabase _database = new();

        public RadioStationServiceTests()
        {
            using var context = _database.CreateContext();
            context.Database.Migrate();
        }

        private RadioStationService CreateService() =>
            new(_database.Services().GetRequiredService<IDbContextFactory<AutomaticaContext>>(), NullLogger<RadioStationService>.Instance);

        private static RadioStation Station(string key, string name = "Mine", string url = "https://mine.example/live") =>
            new() { Key = key, Name = name, StreamUrl = url };

        [Fact]
        public void WithoutSetting_HasTheBuiltInStations()
        {
            var service = CreateService();

            Assert.Equal(RadioStationCatalog.BuiltIn.Stations.Count, service.Stations.Count);
            Assert.Empty(service.CustomStations);
            Assert.Equal("at-oe3", service.Find("at-oe3")?.Key);
        }

        [Fact]
        public async Task Save_StoresTheHiddenSetting_AndAddsTheStations()
        {
            var service = CreateService();

            var saved = await service.SaveCustomStationsAsync(new[]
            {
                new RadioStation { Key = " My-Radio ", Name = " My radio ", StreamUrl = "http://my.example/live", Country = "at", TuneInId = "1234", Genre = " " },
                Station("at-oe3", "Ö3 (eigener Stream)", "https://oe3.example/hq")
            });

            Assert.Equal(new[] { "my-radio", "at-oe3" }, saved.Select(a => a.Key));
            Assert.Equal("My radio", saved[0].Name);
            Assert.Equal("AT", saved[0].Country);
            Assert.Equal("s1234", saved[0].TuneInId);
            Assert.Null(saved[0].Genre);
            Assert.All(saved, a => Assert.True(a.IsCustom));

            // a custom station replaces the built-in one with its key
            Assert.Equal(RadioStationCatalog.BuiltIn.Stations.Count + 1, service.Stations.Count);
            Assert.Equal("https://oe3.example/hq", service.Find("at-oe3").StreamUrl);
            Assert.Equal("my-radio", service.Find("My radio").Key);

            await using (var context = _database.CreateContext())
            {
                var setting = await context.Settings.SingleAsync(a => a.ValueKey == RadioStationService.SettingKey);
                Assert.False(setting.IsVisible);
                Assert.Contains("\"key\":\"my-radio\"", setting.ValueText);
            }

            // a second service (restart) reads them again
            var restarted = CreateService();
            Assert.Equal(2, restarted.CustomStations.Count);
            Assert.Equal("https://oe3.example/hq", restarted.Find("at-oe3").StreamUrl);
        }

        [Fact]
        public async Task Save_Empty_RemovesTheCustomStations()
        {
            var service = CreateService();
            await service.SaveCustomStationsAsync(new[] { Station("mine") });

            await service.SaveCustomStationsAsync(Array.Empty<RadioStation>());

            Assert.Empty(service.CustomStations);
            Assert.Null(CreateService().Find("mine"));
        }

        [Theory]
        [InlineData("Bad Key", "Mine", "https://a.example/x", null, null)]
        [InlineData("ok", " ", "https://a.example/x", null, null)]
        [InlineData("ok", "Mine", "ftp://a.example/x", null, null)]
        [InlineData("ok", "Mine", "https://a.example/x", "javascript:alert(1)", null)]
        [InlineData("ok", "Mine", "https://a.example/x", null, "tunein")]
        public async Task Save_InvalidStation_Throws_AndKeepsTheOldOnes(string key, string name, string url, string logo, string tuneIn)
        {
            var service = CreateService();
            await service.SaveCustomStationsAsync(new[] { Station("mine") });

            await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCustomStationsAsync(new[]
            {
                new RadioStation { Key = key, Name = name, StreamUrl = url, Logo = logo, TuneInId = tuneIn }
            }));

            Assert.Equal("mine", Assert.Single(CreateService().CustomStations).Key);
        }

        [Fact]
        public async Task Save_DuplicateKey_Throws()
        {
            var service = CreateService();

            var error = await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCustomStationsAsync(new[] { Station("mine"), Station("MINE") }));

            Assert.Contains("twice", error.Message);
        }

        [Fact]
        public void ParseCustom_BrokenJson_IsEmpty()
        {
            Assert.Empty(RadioStationService.ParseCustom("{not json"));
            Assert.Empty(RadioStationService.ParseCustom(null));
            Assert.Single(RadioStationService.ParseCustom("[{\"key\":\"a\",\"name\":\"A\",\"streamUrl\":\"https://a.example\"},{\"key\":\"B B\",\"streamUrl\":\"https://a.example\"}]"));
        }

        [Fact]
        public void DatabaseNotReadable_UsesTheBuiltInStations()
        {
            var broken = new SqliteTestDatabase();
            try
            {
                // no migration: the settings table does not exist
                var service = new RadioStationService(broken.Services().GetRequiredService<IDbContextFactory<AutomaticaContext>>(), NullLogger<RadioStationService>.Instance);

                Assert.Equal(RadioStationCatalog.BuiltIn.Stations.Count, service.Stations.Count);
            }
            finally
            {
                broken.Dispose();
            }
        }

        public void Dispose()
        {
            _database.Dispose();
        }
    }
}
