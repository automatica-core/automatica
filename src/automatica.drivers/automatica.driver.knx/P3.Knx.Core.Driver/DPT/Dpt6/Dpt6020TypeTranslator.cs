using Automatica.Core.Driver.Utility;

namespace P3.Knx.Core.Driver.DPT.Dpt6
{
    public class Dpt6020Value
    {
        public bool A { get; }
        public bool B { get; }
        public bool C { get; }
        public bool D { get; }
        public bool E { get; }

        public byte Mode { get; set; }

        public Dpt6020Value(byte value)
        {
            A = Utils.IsBitSet(value, 7);
            B = Utils.IsBitSet(value, 6);
            C = Utils.IsBitSet(value, 5);
            D = Utils.IsBitSet(value, 4);
            E = Utils.IsBitSet(value, 3);

            Mode = Utils.SetBitsTo0(value, 0xF8);
        }

        public byte ToByte()
        {
            var value = Utils.SetBitsTo0(Mode, 0xF8);

            value = Utils.SetBitIfTrue(value, 7, A);
            value = Utils.SetBitIfTrue(value, 6, B);
            value = Utils.SetBitIfTrue(value, 5, C);
            value = Utils.SetBitIfTrue(value, 4, D);
            value = Utils.SetBitIfTrue(value, 3, E);

            return value;
        }
    }
    internal class Dpt6020TypeTranslator : Dpt6Translator
    {
        public override string[] Ids => new[] { "6.020" };

        public override object ConvertFromBusValue(int value)
        {
            return new Dpt6020Value((byte)value);
        }
    }
}
