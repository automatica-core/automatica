using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace P3.Driver.OpenWeatherMap.Tests
{
    internal sealed record Day(long Dt, double TempDay, double TempMin, double TempMax, int Humidity, int Pressure,
        double WindSpeed, int WindDeg, int Clouds, double Pop, string Description, string Summary);

    internal static class OneCallJson
    {
        // fixed instants, no dependency on the current date
        public const long Day1 = 1793952000; // 2026-11-06T08:00:00Z
        public const long Day2 = Day1 + 86400;
        public const long Day3 = Day2 + 86400;
        public const long Day4 = Day3 + 86400;

        private static string D(double value) => value.ToString(CultureInfo.InvariantCulture);

        public static string Build(IEnumerable<Day> days, double currentTemp = 12.5, int currentHumidity = 71, int currentWindDeg = 225,
            double currentWindSpeed = 3.6, int currentPressure = 1017)
        {
            var daily = string.Join(",", days.Select(d =>
                "{" +
                $"\"dt\":{d.Dt},\"sunrise\":{d.Dt - 3600},\"sunset\":{d.Dt + 32400},\"moonrise\":0,\"moonset\":0,\"moon_phase\":0.5," +
                $"\"temp\":{{\"day\":{D(d.TempDay)},\"min\":{D(d.TempMin)},\"max\":{D(d.TempMax)},\"night\":1,\"eve\":2,\"morn\":3}}," +
                "\"feels_like\":{\"day\":1,\"night\":1,\"eve\":1,\"morn\":1}," +
                $"\"pressure\":{d.Pressure},\"humidity\":{d.Humidity},\"dew_point\":1.1,\"wind_speed\":{D(d.WindSpeed)},\"wind_deg\":{d.WindDeg}," +
                $"\"clouds\":{d.Clouds},\"uvi\":1.2,\"pop\":{D(d.Pop)}," +
                $"\"weather\":[{{\"id\":800,\"main\":\"Clear\",\"description\":\"{d.Description}\",\"icon\":\"01d\"}}]," +
                $"\"summary\":\"{d.Summary}\"" +
                "}"));

            return "{\"lat\":48.3069,\"lon\":14.2858,\"timezone\":\"Europe/Vienna\",\"timezone_offset\":3600," +
                   $"\"current\":{{\"dt\":{Day1},\"sunrise\":{Day1 - 3600},\"sunset\":{Day1 + 32400},\"temp\":{D(currentTemp)},\"feels_like\":11.0," +
                   $"\"pressure\":{currentPressure},\"humidity\":{currentHumidity},\"dew_point\":7.2,\"uvi\":0.5,\"clouds\":40,\"visibility\":10000," +
                   $"\"wind_speed\":{D(currentWindSpeed)},\"wind_deg\":{currentWindDeg}," +
                   "\"weather\":[{\"id\":802,\"main\":\"Clouds\",\"description\":\"scattered clouds\",\"icon\":\"03d\"}]}," +
                   $"\"daily\":[{daily}]}}";
        }

        public static IReadOnlyList<Day> FourDays() => new[]
        {
            new Day(Day1, 10, 4, 14, 60, 1010, 2.0, 350, 20, 0.1, "clear sky", "first"),
            new Day(Day2, 12, 6, 16, 80, 1020, 4.0, 10, 40, 0.3, "few clouds", "second"),
            new Day(Day3, 30, -5, 35, 10, 990, 20.0, 180, 100, 0.9, "storm", "third"),
            new Day(Day4, 30, -5, 35, 10, 990, 20.0, 180, 100, 0.9, "storm", "fourth"),
        };
    }
}
