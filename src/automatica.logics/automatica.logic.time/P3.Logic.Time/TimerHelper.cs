using System;
using System.Globalization;

namespace P3.Logic.Time
{
    internal static class TimerHelper
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
