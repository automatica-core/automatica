namespace P3.Knx.Core.Driver.DPT.Dpt9
{
    internal class Dpt9Type2 : Dpt9Translator
    {
        public override string[] Ids => new[] { "9.002", "9.003", "9.010", "9.020", "9.021", "9.023", "9.024", "9.025" };

        public override decimal ValidateMinMax(decimal value)
        {
            if (value < -670760)
            {
                return -670760;
            }
            if (value > 670760)
            {
                return 670760;
            }
            return value;
        }
    }
}
