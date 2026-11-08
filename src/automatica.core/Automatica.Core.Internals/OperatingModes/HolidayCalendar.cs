using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Core.Internals.OperatingModes
{
    /// <summary>
    /// A public holiday. <see cref="Key"/> is the translation key in the UI (OPERATING_MODES.HOLIDAY.&lt;KEY&gt;).
    /// </summary>
    public sealed record Holiday(string Key, DateOnly Date);

    public sealed record HolidayRegion(string Code, string Name);

    public sealed record HolidayCountry(string Code, string Name, IReadOnlyList<HolidayRegion> Regions);

    /// <summary>
    /// Predefined public holidays per country (and region), calculated offline. Holidays with an Easter reference use the
    /// Gregorian Easter date (Gauss / Meeus algorithm).
    /// </summary>
    public static class HolidayCalendar
    {
        private sealed record Rule(string Key, Func<int, DateOnly> Date, params string[] Regions)
        {
            public bool AppliesTo(string region) => Regions.Length == 0 || (!string.IsNullOrEmpty(region) && Regions.Contains(region, StringComparer.OrdinalIgnoreCase));
        }

        private static Func<int, DateOnly> Fixed(int month, int day) => year => new DateOnly(year, month, day);
        private static Func<int, DateOnly> Easter(int offset) => year => EasterSunday(year).AddDays(offset);

        private static readonly Rule NewYear = new("NEW_YEAR", Fixed(1, 1));
        private static readonly Rule Epiphany = new("EPIPHANY", Fixed(1, 6));
        private static readonly Rule GoodFriday = new("GOOD_FRIDAY", Easter(-2));
        private static readonly Rule EasterSun = new("EASTER_SUNDAY", Easter(0));
        private static readonly Rule EasterMonday = new("EASTER_MONDAY", Easter(1));
        private static readonly Rule LabourDay = new("LABOUR_DAY", Fixed(5, 1));
        private static readonly Rule Ascension = new("ASCENSION", Easter(39));
        private static readonly Rule WhitSunday = new("WHIT_SUNDAY", Easter(49));
        private static readonly Rule WhitMonday = new("WHIT_MONDAY", Easter(50));
        private static readonly Rule CorpusChristi = new("CORPUS_CHRISTI", Easter(60));
        private static readonly Rule Assumption = new("ASSUMPTION", Fixed(8, 15));
        private static readonly Rule AllSaints = new("ALL_SAINTS", Fixed(11, 1));
        private static readonly Rule ImmaculateConception = new("IMMACULATE_CONCEPTION", Fixed(12, 8));
        private static readonly Rule ChristmasEve = new("CHRISTMAS_EVE", Fixed(12, 24));
        private static readonly Rule Christmas = new("CHRISTMAS", Fixed(12, 25));
        private static readonly Rule StStephen = new("ST_STEPHEN", Fixed(12, 26));
        private static readonly Rule NewYearsEve = new("NEW_YEARS_EVE", Fixed(12, 31));

        private static readonly Dictionary<string, (string Name, HolidayRegion[] Regions, Rule[] Rules)> Countries = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AT"] = ("Österreich", Array.Empty<HolidayRegion>(), new[]
            {
                NewYear, Epiphany, EasterMonday, LabourDay, Ascension, WhitMonday, CorpusChristi, Assumption,
                new Rule("NATIONAL_DAY_AT", Fixed(10, 26)), AllSaints, ImmaculateConception, Christmas, StStephen
            }),
            ["DE"] = ("Deutschland", new[]
            {
                new HolidayRegion("BW", "Baden-Württemberg"), new HolidayRegion("BY", "Bayern"), new HolidayRegion("BE", "Berlin"),
                new HolidayRegion("BB", "Brandenburg"), new HolidayRegion("HB", "Bremen"), new HolidayRegion("HH", "Hamburg"),
                new HolidayRegion("HE", "Hessen"), new HolidayRegion("MV", "Mecklenburg-Vorpommern"), new HolidayRegion("NI", "Niedersachsen"),
                new HolidayRegion("NW", "Nordrhein-Westfalen"), new HolidayRegion("RP", "Rheinland-Pfalz"), new HolidayRegion("SL", "Saarland"),
                new HolidayRegion("SN", "Sachsen"), new HolidayRegion("ST", "Sachsen-Anhalt"), new HolidayRegion("SH", "Schleswig-Holstein"),
                new HolidayRegion("TH", "Thüringen")
            }, new[]
            {
                NewYear,
                Epiphany with { Regions = new[] { "BW", "BY", "ST" } },
                new Rule("WOMENS_DAY", Fixed(3, 8), "BE", "MV"),
                GoodFriday,
                EasterSun with { Regions = new[] { "BB" } },
                EasterMonday, LabourDay, Ascension,
                WhitSunday with { Regions = new[] { "BB" } },
                WhitMonday,
                CorpusChristi with { Regions = new[] { "BW", "BY", "HE", "NW", "RP", "SL" } },
                Assumption with { Regions = new[] { "SL", "BY" } },
                new Rule("CHILDRENS_DAY", Fixed(9, 20), "TH"),
                new Rule("GERMAN_UNITY", Fixed(10, 3)),
                new Rule("REFORMATION_DAY", Fixed(10, 31), "BB", "HB", "HH", "MV", "NI", "SN", "ST", "SH", "TH"),
                AllSaints with { Regions = new[] { "BW", "BY", "NW", "RP", "SL" } },
                new Rule("REPENTANCE_DAY", RepentanceDay, "SN"),
                Christmas, StStephen
            }),
            ["CH"] = ("Schweiz", Array.Empty<HolidayRegion>(), new[]
            {
                NewYear, new Rule("BERCHTOLDS_DAY", Fixed(1, 2)), GoodFriday, EasterMonday, Ascension, WhitMonday,
                new Rule("NATIONAL_DAY_CH", Fixed(8, 1)), Christmas, StStephen
            }),
            ["LI"] = ("Liechtenstein", Array.Empty<HolidayRegion>(), new[]
            {
                NewYear, new Rule("BERCHTOLDS_DAY", Fixed(1, 2)), Epiphany, new Rule("CANDLEMAS", Fixed(2, 2)), new Rule("ST_JOSEPH", Fixed(3, 19)),
                EasterMonday, LabourDay, Ascension, WhitMonday, CorpusChristi, new Rule("NATIONAL_DAY_LI", Fixed(8, 15)),
                new Rule("NATIVITY_OF_MARY", Fixed(9, 8)), AllSaints, ImmaculateConception, ChristmasEve, Christmas, StStephen, NewYearsEve
            }),
            ["IT"] = ("Italia", new[] { new HolidayRegion("BZ", "Südtirol / Alto Adige") }, new[]
            {
                NewYear, Epiphany, EasterSun, EasterMonday, new Rule("LIBERATION_DAY_IT", Fixed(4, 25)), LabourDay,
                WhitMonday with { Regions = new[] { "BZ" } },
                new Rule("REPUBLIC_DAY_IT", Fixed(6, 2)), Assumption, AllSaints, ImmaculateConception, Christmas, StStephen
            })
        };

        public static IReadOnlyList<HolidayCountry> SupportedCountries =>
            Countries.Select(a => new HolidayCountry(a.Key, a.Value.Name, a.Value.Regions)).OrderBy(a => a.Code).ToList();

        public static bool IsSupported(string country) => !string.IsNullOrEmpty(country) && Countries.ContainsKey(country);

        /// <summary>
        /// The public holidays of a year, sorted by date. Unknown countries have none.
        /// </summary>
        public static IReadOnlyList<Holiday> For(string country, string region, int year)
        {
            if (string.IsNullOrEmpty(country) || !Countries.TryGetValue(country, out var data) || year < 1583 || year > 9999)
            {
                return Array.Empty<Holiday>();
            }

            return data.Rules
                .Where(a => a.AppliesTo(region))
                .Select(a => new Holiday(a.Key, a.Date(year)))
                .OrderBy(a => a.Date)
                .ToList();
        }

        /// <summary>
        /// Gregorian Easter Sunday (anonymous Gregorian algorithm, Meeus/Jones/Butcher)
        /// </summary>
        public static DateOnly EasterSunday(int year)
        {
            var a = year % 19;
            var b = year / 100;
            var c = year % 100;
            var d = b / 4;
            var e = b % 4;
            var f = (b + 8) / 25;
            var g = (b - f + 1) / 3;
            var h = (19 * a + b - d - g + 15) % 30;
            var i = c / 4;
            var k = c % 4;
            var l = (32 + 2 * e + 2 * i - h - k) % 7;
            var m = (a + 11 * h + 22 * l) / 451;
            var month = (h + l - 7 * m + 114) / 31;
            var day = (h + l - 7 * m + 114) % 31 + 1;
            return new DateOnly(year, month, day);
        }

        /// <summary>
        /// Buß- und Bettag: the Wednesday before 23 November
        /// </summary>
        private static DateOnly RepentanceDay(int year)
        {
            var date = new DateOnly(year, 11, 22);
            while (date.DayOfWeek != DayOfWeek.Wednesday)
            {
                date = date.AddDays(-1);
            }
            return date;
        }
    }
}
