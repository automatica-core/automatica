using System;
using System.Linq;
using Automatica.Core.Internals.OperatingModes;
using Xunit;

namespace Automatica.Core.Tests.OperatingModes
{
    public class HolidayCalendarTests
    {
        [Theory]
        [InlineData(2024, 3, 31)]
        [InlineData(2025, 4, 20)]
        [InlineData(2026, 4, 5)]
        [InlineData(2027, 3, 28)]
        [InlineData(2038, 4, 25)]
        public void EasterSunday(int year, int month, int day)
        {
            Assert.Equal(new DateOnly(year, month, day), HolidayCalendar.EasterSunday(year));
        }

        [Fact]
        public void Austria2026()
        {
            var holidays = HolidayCalendar.For("AT", "", 2026);

            Assert.Equal(13, holidays.Count);
            Assert.Contains(holidays, a => a.Key == "EASTER_MONDAY" && a.Date == new DateOnly(2026, 4, 6));
            Assert.Contains(holidays, a => a.Key == "ASCENSION" && a.Date == new DateOnly(2026, 5, 14));
            Assert.Contains(holidays, a => a.Key == "WHIT_MONDAY" && a.Date == new DateOnly(2026, 5, 25));
            Assert.Contains(holidays, a => a.Key == "CORPUS_CHRISTI" && a.Date == new DateOnly(2026, 6, 4));
            Assert.Contains(holidays, a => a.Key == "NATIONAL_DAY_AT" && a.Date == new DateOnly(2026, 10, 26));
            Assert.Equal(holidays.OrderBy(a => a.Date), holidays);
        }

        [Fact]
        public void Germany_RegionalHolidays()
        {
            var nationwide = HolidayCalendar.For("DE", "", 2026);
            Assert.DoesNotContain(nationwide, a => a.Key == "CORPUS_CHRISTI");
            Assert.Contains(nationwide, a => a.Key == "GERMAN_UNITY");

            var bavaria = HolidayCalendar.For("DE", "BY", 2026);
            Assert.Contains(bavaria, a => a.Key == "CORPUS_CHRISTI");
            Assert.Contains(bavaria, a => a.Key == "EPIPHANY");
            Assert.DoesNotContain(bavaria, a => a.Key == "REFORMATION_DAY");

            var saxony = HolidayCalendar.For("de", "sn", 2026);
            Assert.Contains(saxony, a => a.Key == "REPENTANCE_DAY" && a.Date == new DateOnly(2026, 11, 18));
            Assert.Contains(saxony, a => a.Key == "REFORMATION_DAY");
        }

        [Fact]
        public void UnknownCountry_HasNoHolidays()
        {
            Assert.Empty(HolidayCalendar.For("", "", 2026));
            Assert.Empty(HolidayCalendar.For("XX", "", 2026));
            Assert.Contains(HolidayCalendar.SupportedCountries, a => a.Code == "DE" && a.Regions.Count == 16);
        }
    }
}
