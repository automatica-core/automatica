namespace P3.Knx.Core.Driver.DPT.Dpt9
{
    internal class Dpt9004TypeTranslator : Dpt9Translator
    {
        public override string[] Ids => new[] { "9.004", "9.005", "9.006", "9.007", "9.008" };

        public override decimal ValidateMinMax(decimal value)
        {
            if (value < 0)
            {
                return 0;
            }
            if (value > 670760)
            {
                return 670760;
            }
            return value;
        }
    }
}
