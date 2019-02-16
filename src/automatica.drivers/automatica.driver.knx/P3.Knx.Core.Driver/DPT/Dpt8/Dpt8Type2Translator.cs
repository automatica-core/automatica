using System;
using P3.Knx.Core.DPT.Base;

namespace P3.Knx.Core.DPT.Dpt8
{
    internal class Dpt8Type2Translator : Dpt8Translator
    {
        public override string[] Ids => new[] { "8.003" };

        public override object ConvertFromBusValue(int value)
        {
            return (double)value / 100;
        }

        public virtual short ConvertToBusValueInternal(decimal value)
        {
            var busValue = value * 100;

            if (busValue > ushort.MaxValue)
            {
                return short.MaxValue;
            }

            return (short)busValue;
        }


        public override byte[] ToDataPoint(object value)
        {
            var input = GetValueAsDecimal(value);


            if (!input.HasValue)
            {
                throw new ToDataPointException("value has invalid type");
            }

            var shortValue = ValidateMinMax(ConvertToBusValueInternal(input.Value));

            var dataPoint = BitConverter.GetBytes(shortValue);

            return dataPoint;
        }
    }
}
