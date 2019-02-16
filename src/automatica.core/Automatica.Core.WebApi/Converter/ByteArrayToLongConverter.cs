using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Automatica.Core.WebApi.Converter
{
    public class ByteArrayToLongConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is byte[] byteArray)
            {
                writer.WriteValue(BitConverter.ToInt64(byteArray, 0));
            }
            else
            {
                writer.WriteValue(value);
            }

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value is long longValue)
            {
                var bytes = BitConverter.GetBytes(longValue);

                if (bytes.Length != 16)
                {
                    var byteArray = new byte[16];
                    Span<byte> x = bytes;
                    return x.Slice(0, 16).ToArray();
                }

                return bytes;
            }
            return existingValue;
        }

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(Byte[]) || objectType == typeof(byte[]))
            {
                return true;
            }
            return false;
        }
    }
}
