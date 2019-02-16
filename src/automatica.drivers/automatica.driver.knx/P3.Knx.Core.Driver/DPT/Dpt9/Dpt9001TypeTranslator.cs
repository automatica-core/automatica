using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Knx.Core.DPT.Dpt9
{
    internal class Dpt9001TypeTranslator : Dpt9Translator
    {
        public override string[] Ids => new[] { "9.001" };

        public override decimal ValidateMinMax(decimal value)
        {
            if (value < -273)
            {
                return -273;
            }
            if (value > 670760)
            {
                return 670760;
            }
            return value;
        }
    }
}
