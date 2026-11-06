using System.Globalization;
using System.Text.Json;
using Automatica.Core.Base.IO;

namespace Automatica.Core.Mcp.Tools
{
    internal static class McpValues
    {
        /// <summary>
        /// Driver values are mostly primitives, but some drivers dispatch their own objects: keep the result JSON friendly.
        /// </summary>
        public static object ToJsonValue(object value)
        {
            return value switch
            {
                null => null,
                bool or string or double or float or decimal or int or long or short or byte or uint or ulong or ushort or sbyte => value,
                DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
                Enum enumValue => enumValue.ToString(),
                Guid guid => guid.ToString(),
                JsonElement element => element.Clone(),
                _ => value.ToString()
            };
        }

        public static CurrentValue ToCurrentValue(DispatchValue value)
        {
            if (value == null)
            {
                return null;
            }

            return new CurrentValue(ToJsonValue(value.Value), value.Timestamp, value.ValueSource.ToString());
        }

        /// <summary>
        /// Converts a JSON value from the client into what the dispatcher expects (bool, double or string).
        /// </summary>
        public static object FromJson(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => value.GetDouble(),
                JsonValueKind.String => ParseString(value.GetString()),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => throw new ArgumentException("value must be a boolean, a number or a string")
            };
        }

        private static object ParseString(string text)
        {
            if (bool.TryParse(text, out var boolValue))
            {
                return boolValue;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            return text;
        }
    }

    public record CurrentValue(object Value, DateTime Timestamp, string Source);
}
