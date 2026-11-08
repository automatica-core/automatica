using System;
using System.Globalization;

namespace P3.Logic.Operations.Climate.Common
{
    /// <summary>
    /// Tolerant conversion of input and parameter values (numbers, bools, numeric strings, enum strings of the parameter grid)
    /// </summary>
    public static class ClimateValue
    {
        public static double? ToDouble(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        return null;
                    }
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var inv))
                    {
                        return Valid(inv);
                    }
                    if (bool.TryParse(s, out var sb))
                    {
                        return sb ? 1 : 0;
                    }
                    return null;
                case IConvertible c:
                    try
                    {
                        return Valid(c.ToDouble(CultureInfo.InvariantCulture));
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }

        public static bool ToBool(object value)
        {
            var d = ToDouble(value);
            return d.HasValue && d.Value != 0;
        }

        public static int? ToInt(object value)
        {
            var d = ToDouble(value);
            return d.HasValue ? (int)Math.Round(d.Value) : null;
        }

        private static double? Valid(double value) => double.IsNaN(value) || double.IsInfinity(value) ? null : value;

        /// <summary>
        /// Rounds analog outputs so that tiny floating point differences do not produce new values
        /// </summary>
        public static double Round(double value, int digits = 2) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
    }
}
