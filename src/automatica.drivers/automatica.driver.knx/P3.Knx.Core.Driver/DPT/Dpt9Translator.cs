using System;
using System.Globalization;
using P3.Knx.Core.DPT.Base;

namespace P3.Knx.Core.DPT
{
    internal class Dpt9Translator : Dpt
    {
        public override string[] Ids => new[] { "9.*" };

        public virtual decimal ValidateMinMax(decimal value)
        {
            return value;
        }

        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 2)
            {
                throw new FromDataPointException("data is invalid");
            }
            // DPT bits high byte: MEEEEMMM, low byte: MMMMMMMM
            // first M is signed state from two's complement notation

            int val;
            uint m = (uint) ((data[0] & 0x07) << 8) | (data[1]);
            bool signed = ((data[0] & 0x80) >> 7) == 1;

            if (signed)
            {
                // change for two's complement notation and use only mantissa bytes
                m = m - 1;
                m = ~(m);
                m = m & (0x07FF);
                val = (int) (m * -1);
            }
            else
            {
                val = (int) m;
            }

            int power = (data[0] & 0x78) >> 3;

            double calc = 0.01d * val;

            return ValidateMinMax((decimal) Math.Round(calc * Math.Pow(2, power), 2));
        }
        public override byte[] ToDataPoint(object value)
        {
            var dataPoint = new byte[] {0x00, 0x00, 0x00 };

            var dValue = GetValueAsDecimal(value);

            if(!dValue.HasValue)
            {
                throw new ToDataPointException("value has invalid type");
            }

            dValue = ValidateMinMax(dValue.Value);

            // value will be multiplied by 0.01
            decimal v = Math.Round(dValue.Value * 100m);
            // mantissa only holds 11 bits for value, so, check if exponet is required
            int e = 0;
            while (v < -2048m)
            {
                v = v / 2;
                e++;
            }
            while (v > 2047m)
            {
                v = v / 2;
                e++;
            }

            int mantissa;
            bool signed;
            if (v < 0)
            {
                // negative value > two's complement
                signed = true;
                mantissa = ((int) v * -1);
                mantissa = ~mantissa;
                mantissa = mantissa + 1;
            }
            else
            {
                signed = false;
                mantissa = (int) v;
            }

            // signed value > enable first bit
            if (signed)
                dataPoint[1] = 0x80;

            var byte0 = dataPoint[1];
            byte0 = ((byte)(byte0 | ((e & 0x0F) << 3)));
            byte0 = ((byte)(byte0 | ((mantissa >> 8) & 0x07)));

            dataPoint[1] = byte0;
            dataPoint[2] = ((byte) mantissa);

            return dataPoint;
        }
    }
}
