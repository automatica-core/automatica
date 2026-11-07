using System.Globalization;
using System.Text.Json;
using Automatica.Core.Automation.Definition;

namespace Automatica.Core.Automation.Engine
{
    /// <summary>
    /// Conversion and comparison of datapoint values (bool, numbers, strings, JSON values of the definition).
    /// Booleans compare with 0/1 and "on"/"off", numbers with a small tolerance, strings case insensitive.
    /// </summary>
    public static class AutomationValues
    {
        private const double Epsilon = 1e-9;

        public static object FromJson(JsonElement? element)
        {
            if (element == null)
            {
                return null;
            }

            var value = element.Value;
            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => value.GetDouble(),
                JsonValueKind.String => value.GetString(),
                _ => null
            };
        }

        /// <summary>
        /// Value as stored in a run step / sent to the client: primitives only
        /// </summary>
        public static object ToPlain(object value)
        {
            return value switch
            {
                null => null,
                bool or string or double or float or decimal or int or long or short or byte or uint or ulong or ushort or sbyte => value,
                JsonElement element => FromJson(element),
                DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
                Enum enumValue => enumValue.ToString(),
                _ => value.ToString()
            };
        }

        public static string Format(object value)
        {
            return ToPlain(value) switch
            {
                null => "",
                bool b => b ? "on" : "off",
                double d => d.ToString("0.##", CultureInfo.InvariantCulture),
                float f => f.ToString("0.##", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                var other => other.ToString()
            };
        }

        public static bool TryToBool(object value, out bool result)
        {
            switch (ToPlain(value))
            {
                case bool b:
                    result = b;
                    return true;
                case string s:
                    switch (s.Trim().ToLowerInvariant())
                    {
                        case "true":
                        case "on":
                        case "1":
                            result = true;
                            return true;
                        case "false":
                        case "off":
                        case "0":
                            result = false;
                            return true;
                    }
                    break;
                case var other when other != null && TryToDouble(other, out var number):
                    result = Math.Abs(number) > Epsilon;
                    return true;
            }

            result = false;
            return false;
        }

        public static bool TryToDouble(object value, out double result)
        {
            switch (ToPlain(value))
            {
                case bool b:
                    result = b ? 1 : 0;
                    return true;
                case string s:
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
                case IConvertible convertible:
                    try
                    {
                        result = convertible.ToDouble(CultureInfo.InvariantCulture);
                        return !double.IsNaN(result);
                    }
                    catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                    {
                        break;
                    }
            }

            result = 0;
            return false;
        }

        public static bool AreEqual(object a, object b)
        {
            a = ToPlain(a);
            b = ToPlain(b);

            if (a == null || b == null)
            {
                return a == null && b == null;
            }

            if (a is bool || b is bool)
            {
                return TryToBool(a, out var boolA) && TryToBool(b, out var boolB) && boolA == boolB;
            }

            if (IsNumber(a) || IsNumber(b))
            {
                if (TryToDouble(a, out var numberA) && TryToDouble(b, out var numberB))
                {
                    return Math.Abs(numberA - numberB) <= Epsilon * Math.Max(1, Math.Abs(numberA));
                }
                return false;
            }

            return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares two values numerically, then as strings. Null when one of them is unknown.
        /// </summary>
        public static int? Compare(object a, object b)
        {
            a = ToPlain(a);
            b = ToPlain(b);

            if (a == null || b == null)
            {
                return null;
            }

            if (TryToDouble(a, out var numberA) && TryToDouble(b, out var numberB))
            {
                if (Math.Abs(numberA - numberB) <= Epsilon * Math.Max(1, Math.Abs(numberA)))
                {
                    return 0;
                }
                return numberA.CompareTo(numberB);
            }

            return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool Matches(object actual, CompareOperator op, object value, object value2)
        {
            if (ToPlain(actual) == null)
            {
                return false;
            }

            switch (op)
            {
                case CompareOperator.Eq:
                    return AreEqual(actual, value);
                case CompareOperator.Ne:
                    return !AreEqual(actual, value);
                case CompareOperator.Lt:
                    return Compare(actual, value) < 0;
                case CompareOperator.Le:
                    return Compare(actual, value) <= 0;
                case CompareOperator.Gt:
                    return Compare(actual, value) > 0;
                case CompareOperator.Ge:
                    return Compare(actual, value) >= 0;
                case CompareOperator.Between:
                    return Compare(actual, value) >= 0 && Compare(actual, value2) <= 0;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Converts a value for a datapoint of <paramref name="type"/>
        /// </summary>
        public static object ConvertFor(object value, AutomationValueType type)
        {
            value = ToPlain(value);
            switch (type)
            {
                case AutomationValueType.Boolean when TryToBool(value, out var b):
                    return b;
                case AutomationValueType.Number when TryToDouble(value, out var d):
                    return d;
                case AutomationValueType.String when value != null:
                    return value is bool boolValue ? (boolValue ? "true" : "false") : Format(value);
                default:
                    return value;
            }
        }

        private static bool IsNumber(object value)
        {
            return value is double or float or decimal or int or long or short or byte or uint or ulong or ushort or sbyte;
        }

        public static bool TryParseTime(string text, out TimeSpan time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return (TimeSpan.TryParseExact(text.Trim(), new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" }, CultureInfo.InvariantCulture, out time)
                    && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1));
        }
    }
}
