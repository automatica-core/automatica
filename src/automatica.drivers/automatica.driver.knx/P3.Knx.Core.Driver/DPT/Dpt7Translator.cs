using P3.Knx.Core.Driver.DPT.Base;

namespace P3.Knx.Core.Driver.DPT
{
    internal class Dpt7Translator : DptToUShortBase
    {
        public override string[] Ids => new [] {"7.*"};
    }
}
