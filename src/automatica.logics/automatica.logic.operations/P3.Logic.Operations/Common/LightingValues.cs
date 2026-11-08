using System;
using System.Globalization;

namespace P3.Logic.Operations.Common
{
    /// <summary>
    /// Tolerant conversions of input and parameter values (bool, numbers, strings from the UI or a JSON value)
    /// </summary>
    internal static class LightingValues
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
                    if (bool.TryParse(s, out var sb))
                    {
                        return sb;
                    }
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sd) && sd != 0;
                case IConvertible c:
                    try
                    {
                        return c.ToDouble(CultureInfo.InvariantCulture) != 0;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        /// <summary>
        /// The value as double, <paramref name="fallback"/> when it is no number
        /// </summary>
        internal static double ToDouble(object value, double fallback)
        {
            try
            {
                var d = value switch
                {
                    null => fallback,
                    bool b => b ? 1 : 0,
                    string s => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
                    _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
                };
                return double.IsNaN(d) || double.IsInfinity(d) ? fallback : d;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        internal static int ToInt(object value, int fallback)
        {
            var d = ToDouble(value, double.NaN);
            if (double.IsNaN(d))
            {
                return fallback;
            }
            return (int)Math.Round(Math.Clamp(d, int.MinValue, int.MaxValue));
        }

        internal static double Clamp01To100(double value) => Math.Clamp(value, 0, 100);

        /// <summary>
        /// <see cref="System.Timers.Timer.Interval"/> must be &gt; 0 and &lt;= int.MaxValue
        /// </summary>
        internal static double ClampInterval(double milliseconds)
        {
            if (double.IsNaN(milliseconds) || milliseconds < 1)
            {
                return 1;
            }

            return Math.Min(milliseconds, int.MaxValue);
        }
    }
}
