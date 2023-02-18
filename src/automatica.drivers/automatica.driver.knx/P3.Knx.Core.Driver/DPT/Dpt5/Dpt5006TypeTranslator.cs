namespace P3.Knx.Core.Driver.DPT.Dpt5
{
    internal class Dpt5006TypeTranslator : Dpt5Translator
    {
        public override string[] Ids => new[] { "5.006" };

        public override object ConvertFromBusValue(int value)
        {
            if (value == 255)
            {
                return 254;
            }
            return value;
        }

        public override byte ConvertToBusValue(int value)
        {
            if (value == 255)
            {
                return 254;
            }
            return (byte)value;
        }
    }
}
