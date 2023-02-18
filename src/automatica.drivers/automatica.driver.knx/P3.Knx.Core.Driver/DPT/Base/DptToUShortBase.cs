using Automatica.Core.Base.Cryptography;
using System;

namespace P3.Knx.Core.Driver.DPT.Base
{
    public abstract class DptToUShortBase : Dpt
    {
        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 2)
                throw new FromDataPointException($"data input must have 2 bytes ({data?.ToHex(true)})");

            return ConvertFromBusValue(ValidateMinMax(BitConverter.ToUInt16(data, 0)));
        }

        public virtual ushort ValidateMinMax(int value)
        {
            if (value > ushort.MaxValue)
            {
                value = ushort.MaxValue;
            }
            if (value < ushort.MinValue)
            {
                value = ushort.MinValue;
            }
            return (ushort)value;
        }

        public virtual object ConvertFromBusValue(int value)
        {
            return Convert.ToUInt16(value);
        }

        public virtual ushort ConvertToBusValue(int value)
        {
            return Convert.ToUInt16(value);
        }

        public override byte[] ToDataPoint(object value)
        {
            var input = GetValueAsInt(value);

            if (!input.HasValue)
            {
                throw new ToDataPointException("value has invalid type");
            }

            var shortValue = ConvertToBusValue(ValidateMinMax(input.Value));

            var dataPoint = BitConverter.GetBytes(shortValue);

            return dataPoint;
        }
    }
}
