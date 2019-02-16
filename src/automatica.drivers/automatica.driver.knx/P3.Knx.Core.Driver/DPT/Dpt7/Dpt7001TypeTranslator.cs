using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Knx.Core.DPT.Dpt7
{
    internal class Dpt7001TypeTranslator : Dpt7Translator
    {
        public override string[] Ids => new[] { "7.001", "7.002", "7.005", "7.006", "7.007", "7.010", "7.011", "7.012", "7.013" };
    }
}
