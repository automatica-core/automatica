using System;

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

        internal static bool ConvertValueToBool(object value)
        {
            if (value is int intValue)
            {
                return intValue >= 1;
            }
            if (value is bool boolValue2)
            {
                return boolValue2 ? true : false;
            }

            if (int.TryParse(value.ToString(), out int result))
            {
                return result >= 1;
            }
            throw new NotImplementedException();
        }
    }
}
