using Automatica.Core.Base.Cryptography;
using System;
using System.Linq;

namespace P3.Knx.Core.Driver.DPT.Base
{
    public abstract class DptToIntBase : Dpt
    {
        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 4)
                throw new FromDataPointException($"data input must have 4 bytes  ({data?.ToHex(true)})");

            var reversed = data.Reverse().ToArray();
            return ConvertFromBusValue(ValidateMinMax(BitConverter.ToInt32(reversed, 0)));
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
            
            return dataPoint.Reverse().ToArray();
        }
    }
}
