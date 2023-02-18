using P3.Knx.Core.Driver.DPT.Base;

namespace P3.Knx.Core.Driver.DPT
{

    internal class Dpt13Translator : DptToIntBase
    {
        public override string[] Ids => new[] {"13", "13.*"};
    }
}
