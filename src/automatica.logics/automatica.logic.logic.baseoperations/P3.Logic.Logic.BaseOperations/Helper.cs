using System;
using System.Globalization;

namespace P3.Logic.Logic.BaseOperations
{
    internal static class Helper
    {
        internal static int ConvertValueToInt(object value)
        {
            if (value is int intValue)
            {
                return intValue;
            }
            if (value is bool boolValue2)
            {
                return boolValue2 ? 1 : 0;
            }

            if (int.TryParse(value.ToString(), out int result))
            {
                return result;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Interprets a value as boolean: bool as is, numbers are true if they are not 0,
        /// strings "true"/"false" or numbers (invariant culture). Never throws, anything else is false.
        /// </summary>
        internal static bool ConvertValueToBool(object value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool boolValue:
                    return boolValue;
                case string stringValue:
                    if (bool.TryParse(stringValue.Trim(), out var parsedBool))
                    {
                        return parsedBool;
                    }

                    return double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble) && parsedDouble != 0;
                case IConvertible convertible:
                    try
                    {
                        var doubleValue = convertible.ToDouble(CultureInfo.InvariantCulture);
                        return !double.IsNaN(doubleValue) && doubleValue != 0;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }
}
