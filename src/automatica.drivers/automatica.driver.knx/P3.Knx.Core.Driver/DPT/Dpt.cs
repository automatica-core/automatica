using System;
using P3.Knx.Core.DPT.Dpt6;

namespace P3.Knx.Core.DPT
{
    public abstract class Dpt
    {
        public abstract string[] Ids { get; }
        
        public abstract object FromDataPoint(byte[] data);

        public abstract byte[] ToDataPoint(object value);

        public int? GetValueAsInt(object value)
        {
            int input;
            if (value is int i)
                input = i;
            else if (value is float)
                input = (int)(float)value;
            else if (value is long)
                input = (int)(long)value;
            else if (value is double)
                input = (int)(double)value;
            else if (value is decimal)
                input = (int)(decimal)value;
            else if (value is Dpt6020Value dpt6Value)
                return dpt6Value.ToByte();
            else if (value is bool bValue)
                return Convert.ToInt32(bValue);
            else
            {
                return null;
            }
            return input;
        }


        public decimal? GetValueAsDecimal(object value)
        {
            decimal input;
            if (value is int i)
                input = i;
            else if (value is float)
                input = (decimal)(float)value;
            else if (value is long)
                input = (long)value;
            else if (value is double)
                input = (decimal)(double)value;
            else if (value is decimal)
                input = (decimal)value;
            else
            {
                return null;
            }
            return input;
        }
    }
}
