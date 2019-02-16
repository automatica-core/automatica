using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace P3.Driver.IkeaTradfri.Extensions
{
    public class MicrosecondEpochConverter : DateTimeConverterBase
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(((DateTime)value - Epoch).TotalMilliseconds + "000");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) { return DateTime.MinValue; }
            return Epoch.AddMilliseconds((long)reader.Value*1000);
        }
    }
}
