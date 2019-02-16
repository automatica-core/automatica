using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Knx.Core.DPT.Dpt8
{
    internal class Dpt8Type3Translator : Dpt8Type2Translator
    {
        public override string[] Ids => new[] { "8.004" };

        public override object ConvertFromBusValue(int value)
        {
            return (double)value / 10;
        }

        public override short ConvertToBusValueInternal(decimal value)
        {
            var busValue = value * 10;

            if (busValue > ushort.MaxValue)
            {
                return short.MaxValue;
            }

            return (short)busValue;
        }

    }
}
