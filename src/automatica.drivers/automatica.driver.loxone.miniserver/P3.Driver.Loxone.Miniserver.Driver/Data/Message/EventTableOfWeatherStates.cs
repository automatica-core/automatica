using System;
using System.Collections.Generic;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.Message
{
    public class WeatherEntry
    {
        public const int LoxoneStructLength = 68;
        private WeatherEntry(int timestamp, int weatherType, int windDirection, int solarRadiation, int relativeHumidity, double temperature, double perceivedTemperature, double dewPoint, double precipitation, double windSpeed, double barometicPressure)
        {
            Timestamp = timestamp;
            WeatherType = weatherType;
            WindDirection = windDirection;
            SolarRadiation = solarRadiation;
            RelativeHumidity = relativeHumidity;
            Temperature = temperature;
            PerceivedTemperature = perceivedTemperature;
            DewPoint = dewPoint;
            Precipitation = precipitation;
            WindSpeed = windSpeed;
            BarometicPressure = barometicPressure;

            TimestampDate = new DateTime(2009, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Timestamp);
        }
        public static WeatherEntry Parse(Span<byte> data)
        {
            if(data.Length != LoxoneStructLength)
            {
                throw new ArgumentException($"{nameof(WeatherEntry)} must have a length of {LoxoneStructLength}");
            }
            var timestamp = data.Slice(0, 4);
            var weatherType = data.Slice(4, 4);
            var windDirection = data.Slice(8, 4);
            var solarRadiation = data.Slice(12, 4);
            var relativeHumidty = data.Slice(16, 4);
            var temperature = data.Slice(20, 8);
            var perceivedTemperature = data.Slice(28, 8);
            var dewPoint = data.Slice(36, 8);
            var precipitation= data.Slice(44, 8);
            var windSpeed = data.Slice(52, 8);
            var barometicPressure = data.Slice(60, 8);

            return new WeatherEntry(BitConverter.ToInt32(timestamp), 
                BitConverter.ToInt32(weatherType), 
                BitConverter.ToInt32(windDirection), 
                BitConverter.ToInt32(solarRadiation), 
                BitConverter.ToInt32(relativeHumidty), 
                BitConverter.ToDouble(temperature), 
                BitConverter.ToDouble(perceivedTemperature), 
                BitConverter.ToDouble(dewPoint), 
                BitConverter.ToDouble(precipitation), 
                BitConverter.ToDouble(windSpeed), 
                BitConverter.ToDouble(barometicPressure));
        }

        public int Timestamp { get; }
        public int WeatherType { get; }
        public int WindDirection { get; }
        public int SolarRadiation { get; }
        public int RelativeHumidity { get; }
        public double Temperature { get; }
        public double PerceivedTemperature { get; }
        public double DewPoint { get; }
        public double Precipitation { get; }
        public double WindSpeed { get; }
        public double BarometicPressure { get; }

        public DateTime TimestampDate { get; }
    }

    public class Weather
    {
        public const int LoxoneStructLength = 24; //the length of the binary struct
        public Weather(uint lastUpdate)
        {
            LastUpdate = lastUpdate;
            WeatherEntries = new List<WeatherEntry>();
        }

        public IList<WeatherEntry> WeatherEntries { get; }

        public uint LastUpdate { get; }
    }

    public class EventTableOfWeatherStates : BinaryMessage
    {
        public Dictionary<LoxoneUuid, Weather> Values { get; }
        public EventTableOfWeatherStates(Header header) : base(header)
        {
            Values = new Dictionary<LoxoneUuid, Weather>();
        }

        protected override void Parse(Span<byte> data)
        {
            var pos = 0;

            do
            {
                var uuid = data.Slice(pos + 0, 16);
                var lUuid = new LoxoneUuid(uuid);

                var lastUpdate = data.Slice(pos + 16, 4);
                var lastUpdateUint = BitConverter.ToUInt32(lastUpdate);

                var entriesCountB = data.Slice(pos + 20, 4);
                var entriesCount = BitConverter.ToInt32(entriesCountB);

                var weather = new Weather(lastUpdateUint);

                for (int i = 0; i < entriesCount; i++)
                {
                    var entryData = data.Slice(pos + Weather.LoxoneStructLength + (i * WeatherEntry.LoxoneStructLength), WeatherEntry.LoxoneStructLength);
                    var daytimerEntry = WeatherEntry.Parse(entryData);
                    weather.WeatherEntries.Add(daytimerEntry);
                }

                pos += (entriesCount * WeatherEntry.LoxoneStructLength) + Weather.LoxoneStructLength;
                Values.Add(lUuid, weather);

            } while (pos != data.Length);
        }
    }
}
