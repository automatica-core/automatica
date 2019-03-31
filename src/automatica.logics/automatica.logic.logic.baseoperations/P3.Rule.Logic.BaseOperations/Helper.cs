using System;

namespace P3.Rule.Logic.BaseOperations
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
    }
}
