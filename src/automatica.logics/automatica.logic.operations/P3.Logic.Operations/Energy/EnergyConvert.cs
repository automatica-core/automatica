using System;
using System.Globalization;

namespace P3.Logic.Operations.Energy
{
    internal static class EnergyConvert
    {
        /// <summary>
        /// Number from a boxed value (numbers, bools, invariant strings); null when it is no number
        /// </summary>
        internal static double? ToDouble(object value)
        {
            double result;
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    if (bool.TryParse(s, out var sb))
                    {
                        return sb ? 1 : 0;
                    }
                    if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                    {
                        return null;
                    }
                    break;
                case IConvertible c:
                    try
                    {
                        result = c.ToDouble(CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    break;
                default:
                    return null;
            }

            return double.IsNaN(result) || double.IsInfinity(result) ? null : result;
        }

        internal static double ToDouble(object value, double fallback) => ToDouble(value) ?? fallback;

        /// <summary>
        /// A non-negative number, the fallback for missing or invalid values
        /// </summary>
        internal static double ToNonNegative(object value, double fallback) => Math.Max(0, ToDouble(value) ?? fallback);

        internal static bool ToBool(object value, bool fallback = false)
        {
            var d = ToDouble(value);
            return d.HasValue ? d.Value != 0 : fallback;
        }

        /// <summary>
        /// Derives a stable interface id for a load: the last four hex digits of the base id are replaced by the load number and the field
        /// </summary>
        internal static Guid LoadGuid(string baseGuid, int load, int field)
        {
            if (baseGuid.Length != 28)
            {
                throw new ArgumentException("the base must be 28 hex digits (a guid without dashes and its last four digits)", nameof(baseGuid));
            }
            return new Guid($"{baseGuid}{load:x2}{field:x2}");
        }
    }
}
