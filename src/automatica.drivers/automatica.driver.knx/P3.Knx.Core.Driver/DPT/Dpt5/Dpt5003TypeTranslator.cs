using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Knx.Core.DPT.Dpt5
{
    internal class Dpt5003TypeTranslator : Dpt5Translator
    {
        public override string[] Ids => new[] { "5.003" };

        public override byte ConvertToBusValue(int value)
        {
            if (value > 360)
            {
                value = 360;
            }
            var retValue = Math.Ceiling((double)value * 255 / 360);
            return (byte)retValue;
        }

        public override object ConvertFromBusValue(int value)
        {
            return Math.Floor(value * 360.0 / 255.0);
        }
    }

}
