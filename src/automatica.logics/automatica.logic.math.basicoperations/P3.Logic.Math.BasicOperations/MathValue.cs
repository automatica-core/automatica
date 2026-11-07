using System;
using System.Globalization;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("P3.Logic.Math.BasicOperations.Tests")]

namespace P3.Logic.Math.BasicOperations
{
    internal static class MathValue
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

        internal static bool ToBool(object value)
        {
            var d = ToDouble(value);
            return d.HasValue && d.Value != 0;
        }
    }
}
