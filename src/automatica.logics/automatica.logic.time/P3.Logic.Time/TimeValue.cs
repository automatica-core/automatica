using System;
using System.Globalization;

namespace P3.Logic.Time
{
    internal static class TimeValue
    {
        /// <summary>
        /// Interprets a value as number: numbers as is, bool as 1/0, strings in invariant culture.
        /// Never throws, anything else (and NaN) is null.
        /// </summary>
        internal static double? ToDouble(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    if (bool.TryParse(s.Trim(), out var parsedBool))
                    {
                        return parsedBool ? 1 : 0;
                    }
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && !double.IsNaN(parsed) ? parsed : null;
                case IConvertible c:
                    try
                    {
                        var d = c.ToDouble(CultureInfo.InvariantCulture);
                        return double.IsNaN(d) ? null : d;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// A time parameter in seconds, never below <paramref name="minimum"/>; invalid values give <paramref name="fallback"/>
        /// </summary>
        internal static double ToSeconds(object value, double fallback, double minimum = 0)
        {
            var seconds = ToDouble(value) ?? fallback;
            return System.Math.Max(seconds, minimum);
        }
    }
}
