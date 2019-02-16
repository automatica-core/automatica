using System;
using P3.Knx.Core.DPT.Base;

namespace P3.Knx.Core.DPT
{
    internal class Dpt7Translator : DptToUShortBase
    {
        public override string[] Ids => new [] {"7.*"};
    }
}
