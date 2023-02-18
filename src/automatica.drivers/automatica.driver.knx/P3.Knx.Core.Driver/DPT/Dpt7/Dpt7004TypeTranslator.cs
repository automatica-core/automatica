namespace P3.Knx.Core.Driver.DPT.Dpt7
{
    internal class Dpt7004TypeTranslator : Dpt7003TypeTranslator
    {
        public override string[] Ids => new[] { "7.004" };

        public override object ConvertFromBusValue(int value)
        {
            return (double)value / 10;
        }

        public override ushort ConvertToBusValueInternal(decimal value)
        {
            var busValue = value * 10;

            if (busValue > ushort.MaxValue)
            {
                return ushort.MaxValue;
            }

            return (ushort)busValue;
        }
    }
}
