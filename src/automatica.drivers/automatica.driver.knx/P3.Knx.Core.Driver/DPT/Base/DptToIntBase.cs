using System;

namespace P3.Knx.Core.DPT.Base
{
    public abstract class DptToIntBase : Dpt
    {
        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 2)
                throw new FromDataPointException("data input must have 2 bytes");

            return ConvertFromBusValue(ValidateMinMax(BitConverter.ToInt16(data, 0)));
        }

        public virtual int ValidateMinMax(int value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }
            if (value < int.MinValue)
            {
                return int.MinValue;
            }
            return (int)value;
        }

        public virtual object ConvertFromBusValue(int value)
        {
            return Convert.ToInt32(value);
        }

        public virtual int ConvertToBusValue(int value)
        {
            return Convert.ToInt32(value);
        }

        public override byte[] ToDataPoint(object value)
        {
            var input = GetValueAsInt(value);

            if (!input.HasValue)
            {
                throw new ToDataPointException("value has invalid type");
            }

            var intInput = ConvertToBusValue(ValidateMinMax(input.Value));

            var dataPoint = BitConverter.GetBytes(intInput);

            return dataPoint;
        }
    }
}
