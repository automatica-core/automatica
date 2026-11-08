using System;
using System.Globalization;

namespace P3.Logic.StringOperations.Common
{
    /// <summary>
    /// Conversions shared by the text blocks: values arrive as bool, numbers or strings
    /// </summary>
    internal static class TextValue
    {
        /// <summary>
        /// true for bool true, non-zero numbers and the strings "true" / non-zero numbers
        /// </summary>
        internal static bool IsTruthy(object value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool b:
                    return b;
                case string s:
                    if (bool.TryParse(s.Trim(), out var sb))
                    {
                        return sb;
                    }
                    return TryToDouble(s, out var sd) && sd != 0;
                default:
                    return TryToDouble(value, out var d) && d != 0;
            }
        }

        /// <summary>
        /// Numbers, bools (1/0) and numeric strings (invariant, a decimal comma is accepted too)
        /// </summary>
        internal static bool TryToDouble(object value, out double result)
        {
            result = 0;
            switch (value)
            {
                case null:
                    return false;
                case bool b:
                    result = b ? 1 : 0;
                    return true;
                case string s:
                    return TryParse(s, out result);
                case IConvertible c:
                    try
                    {
                        result = c.ToDouble(CultureInfo.InvariantCulture);
                        return !double.IsNaN(result);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                default:
                    return TryParse(value.ToString(), out result);
            }
        }

        internal static bool TryParse(string text, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && !double.IsNaN(result))
            {
                return true;
            }

            // "21,5" - only when there is no other separator, "1,000.5" is not a number here
            if (text.IndexOf(',') >= 0 && text.IndexOf('.') < 0
                && double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                && !double.IsNaN(result))
            {
                return true;
            }

            result = 0;
            return false;
        }

        internal static double ToDouble(object value, double fallback)
        {
            return TryToDouble(value, out var d) ? d : fallback;
        }

        /// <summary>
        /// Text of a value for status texts: numbers invariant and without trailing zeros, bools as 1 / 0
        /// </summary>
        internal static string ToText(object value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case string s:
                    return s;
                case bool b:
                    return b ? "1" : "0";
                case double d:
                    return Format(d);
                case float f:
                    return Format(f);
                case decimal m:
                    return Format((double)m);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        internal static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
            return Math.Round(value, 10).ToString("0.##########", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Value with a fixed number of decimals, text that is not a number stays as it is
        /// </summary>
        internal static string Format(object value, int decimals)
        {
            if (value is string s && !TryParse(s, out _))
            {
                return s;
            }
            if (TryToDouble(value, out var d))
            {
                return d.ToString("F" + Math.Clamp(decimals, 0, 10), CultureInfo.InvariantCulture);
            }
            return ToText(value);
        }

        /// <summary>
        /// Parameter value as text, null for empty ones
        /// </summary>
        internal static string NullIfEmpty(object value)
        {
            var text = value?.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
