using P3.Knx.Core.Driver.DPT.Base;

namespace P3.Knx.Core.Driver.DPT
{
    internal class Dpt6Translator : DptToByteBase
    {
        public override string[] Ids => new[] { "6.*" };


        public override byte ConvertToBusValue(int value)
        {
            return (byte) value;
        }

        public override object ConvertFromBusValue(int value)
        {
            return (sbyte) value;
        }
    }
}