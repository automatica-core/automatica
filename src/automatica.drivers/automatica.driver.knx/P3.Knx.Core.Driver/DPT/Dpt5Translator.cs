using P3.Knx.Core.DPT.Base;


namespace P3.Knx.Core.DPT
{
    // ReSharper disable once InconsistentNaming
    public class Dpt5Translator : DptToByteBase
    {
        public override string[] Ids => new[] { "5.*" };
    }
}