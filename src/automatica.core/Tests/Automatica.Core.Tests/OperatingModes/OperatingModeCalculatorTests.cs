using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Automatica.Core.Internals.OperatingModes;
using Xunit;

namespace Automatica.Core.Tests.OperatingModes
{
    public class OperatingModeCalculatorTests
    {
        private static OperatingModeConfiguration Configuration(Action<OperatingModeConfiguration> setup = null)
        {
            var configuration = OperatingModeConfiguration.CreateDefault();
            configuration.Country = "AT";
            configuration.Modes.Add(new OperatingModeDefinition { Key = "party", Name = "Party", Priority = 500 });
            setup?.Invoke(configuration);
            return OperatingModeConfiguration.Normalize(configuration);
        }

        [Fact]
        public void PublicHoliday_FromTheCalendar()
        {
            var day = OperatingModeCalculator.For(Configuration(), new DateOnly(2026, 12, 25));

            Assert.Equal(new[] { OperatingModeContract.PublicHoliday }, day.Modes);
            Assert.Equal("CHRISTMAS", day.Reasons[OperatingModeContract.PublicHoliday].Single().Key);
            Assert.Empty(OperatingModeCalculator.For(Configuration(), new DateOnly(2026, 12, 23)).Modes);
        }

        [Fact]
        public void DisabledHoliday_IsNotActive()
        {
            var configuration = Configuration(a => a.DisabledHolidays.Add("CHRISTMAS"));

            Assert.Empty(OperatingModeCalculator.For(configuration, new DateOnly(2026, 12, 25)).Modes);
            Assert.Single(OperatingModeCalculator.For(configuration, new DateOnly(2026, 12, 26)).Modes);
        }

        [Fact]
        public void Periods_BothDaysIncluded_SortedByPriority()
        {
            var configuration = Configuration(a =>
            {
                a.Periods.Add(new OperatingPeriod { Name = "Ski", Mode = OperatingModeContract.Vacation, Start = new DateOnly(2026, 12, 24), End = new DateOnly(2027, 1, 2) });
                a.Periods.Add(new OperatingPeriod { Name = "NYE", Mode = "party", Start = new DateOnly(2026, 12, 31), End = new DateOnly(2027, 1, 1) });
            });

            Assert.Empty(OperatingModeCalculator.For(configuration, new DateOnly(2026, 12, 23)).Modes);
            Assert.Equal(new[] { OperatingModeContract.PublicHoliday, OperatingModeContract.Vacation }, OperatingModeCalculator.For(configuration, new DateOnly(2026, 12, 25)).Modes);
            Assert.Equal(new[] { "party", OperatingModeContract.PublicHoliday, OperatingModeContract.Vacation }, OperatingModeCalculator.For(configuration, new DateOnly(2027, 1, 1)).Modes);
            Assert.Equal(new[] { OperatingModeContract.Vacation }, OperatingModeCalculator.For(configuration, new DateOnly(2027, 1, 2)).Modes);
            Assert.Empty(OperatingModeCalculator.For(configuration, new DateOnly(2027, 1, 3)).Modes);
        }

        [Fact]
        public void YearlyPeriod_RepeatsAcrossNewYear()
        {
            var period = new OperatingPeriod { Mode = "party", Start = new DateOnly(2020, 12, 30), End = new DateOnly(2021, 1, 2), Yearly = true };

            Assert.True(period.Covers(new DateOnly(2026, 12, 31)));
            Assert.True(period.Covers(new DateOnly(2027, 1, 2)));
            Assert.False(period.Covers(new DateOnly(2027, 1, 3)));
            Assert.False(period.Covers(new DateOnly(2026, 12, 29)));

            period.Enabled = false;
            Assert.False(period.Covers(new DateOnly(2026, 12, 31)));
        }

        [Fact]
        public void Manual_OverridesTheCalendar()
        {
            var on = Configuration(a => a.Modes.Single(m => m.Key == OperatingModeContract.Vacation).Manual = true);
            var day = OperatingModeCalculator.For(on, new DateOnly(2026, 3, 3));
            Assert.Equal(new[] { OperatingModeContract.Vacation }, day.Modes);
            Assert.Equal("manual", day.Reasons[OperatingModeContract.Vacation].Single().Type);

            var off = Configuration(a => a.Modes.Single(m => m.Key == OperatingModeContract.PublicHoliday).Manual = false);
            Assert.Empty(OperatingModeCalculator.For(off, new DateOnly(2026, 12, 25)).Modes);
        }

        [Fact]
        public void StateJson_ForTheLogics()
        {
            var configuration = Configuration(a => a.Periods.Add(new OperatingPeriod { Mode = OperatingModeContract.Vacation, Start = new DateOnly(2026, 12, 20), End = new DateOnly(2026, 12, 21) }));

            using var json = JsonDocument.Parse(OperatingModeCalculator.StateJson(configuration, new DateOnly(2026, 12, 20), 48.3, 14.3));
            var root = json.RootElement;

            Assert.Equal("2026-12-20", root.GetProperty("today").GetString());
            Assert.Equal(48.3, root.GetProperty("latitude").GetDouble());
            Assert.Equal(OperatingModeContract.Vacation, root.GetProperty("active")[0].GetString());
            Assert.Equal(OperatingModeContract.PublishedDays, root.GetProperty("days").EnumerateObject().Count());
            Assert.Equal(OperatingModeContract.PublicHoliday, root.GetProperty("days").GetProperty("2026-12-25")[0].GetString());
            Assert.Equal("party", root.GetProperty("modes")[0].GetProperty("key").GetString());
        }

        [Fact]
        public void Normalize_AddsBuiltIns_DropsBrokenEntries()
        {
            var configuration = OperatingModeConfiguration.Parse("""
            {
              "country": " de ", "region": "by",
              "modes": [ { "key": "Home Office", "name": "Home office", "priority": 50 }, { "key": "home-office" }, { "key": "" }, null ],
              "periods": [
                { "id": "a", "mode": "home-office", "start": "2026-05-10", "end": "2026-05-01" },
                { "id": "a", "mode": "home-office", "start": "2026-06-01", "end": "2026-06-01" },
                { "id": "b", "mode": "unknown", "start": "2026-06-01", "end": "2026-06-01" }
              ]
            }
            """);

            Assert.Equal("DE", configuration.Country);
            Assert.Equal("BY", configuration.Region);
            Assert.Equal(new[] { "home-office", OperatingModeContract.PublicHoliday, OperatingModeContract.Vacation, OperatingModeContract.SchoolHolidays }, configuration.Modes.Select(a => a.Key));
            Assert.True(configuration.Modes.Single(a => a.Key == OperatingModeContract.Vacation).BuiltIn);
            Assert.Equal(2, configuration.Periods.Count);
            Assert.Equal(new DateOnly(2026, 5, 1), configuration.Periods[0].Start);
            Assert.NotEqual(configuration.Periods[0].Id, configuration.Periods[1].Id);

            var roundTrip = OperatingModeConfiguration.Parse(configuration.ToJson());
            Assert.Equal(configuration.ToJson(), roundTrip.ToJson());
            Assert.Contains("\"start\":\"2026-05-01\"", configuration.ToJson());
        }

        [Theory]
        [InlineData("Home Office", "home-office")]
        [InlineData("  Ferien  Süd ", "ferien-sued")]
        [InlineData("!!!", "")]
        [InlineData(null, "")]
        public void Slug(string value, string expected)
        {
            Assert.Equal(expected, OperatingModeConfiguration.Slug(value));
        }

        [Fact]
        public void Parse_InvalidJson_GivesTheDefaults()
        {
            var configuration = OperatingModeConfiguration.Parse("{not json");
            Assert.Equal(3, configuration.Modes.Count);
            Assert.Empty(configuration.Periods);
        }
    }
}
