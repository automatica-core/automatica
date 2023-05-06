using Automatica.Core.Base.Cryptography;
using System;

namespace P3.Knx.Core.Driver.DPT.Base
{
    public abstract class DptToShortBase : Dpt
    {
        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 2)
                throw new FromDataPointException($"data input must have 2 bytes ({data?.ToHex(true)})");

            return ConvertFromBusValue(ValidateMinMax(BitConverter.ToInt16(data, 0)));
        }

        public virtual short ValidateMinMax(int value)
        {
            if (value > short.MaxValue)
            {
                return short.MaxValue;
            }
            if (value < short.MinValue)
            {
                return short.MinValue;
            }
            return (short)value;
        }

        public virtual object ConvertFromBusValue(int value)
        {
            return Convert.ToInt16(value);
        }

        public virtual short ConvertToBusValue(int value)
        {
            return Convert.ToInt16(value);
        }

        public override byte[] ToDataPoint(object value)
        {
            var input = GetValueAsInt(value);

            if (!input.HasValue)
            {
                throw new ToDataPointException("value has invalid type");
            }

            var shortInput = ConvertToBusValue(ValidateMinMax(input.Value));

            var dataPoint = BitConverter.GetBytes(shortInput);

            return dataPoint;
        }
    }
}
