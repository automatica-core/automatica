using System;

namespace P3.Knx.Core.Driver.DPT.Dpt5
{
    internal class Dpt5001TypeTranslator : Dpt5Translator
    {
        public override string[] Ids => new[] { "5.001" };

        public override object ConvertFromBusValue(int value)
        {
            return value * 100.0 / 255.0;
        }

        public override byte ConvertToBusValue(int value)
        {
            if (value > 100)
            {
                value = 100;
            }
            var retValue = Math.Ceiling((double)value * 255 / 100);
            return (byte)retValue;
        }
    }
}
