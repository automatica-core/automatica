namespace P3.Knx.Core.Driver.DPT.Dpt9
{
    internal class Dpt9027TypeTranslator : Dpt9Translator
    {
        public override string[] Ids => new[] { "9.027" };

        public override decimal ValidateMinMax(decimal value)
        {
            if (value < -459.6m)
            {
                return -459.6m;
            }
            if (value > 670760.96m)
            {
                return 670760.96m;
            }
            return value;
        }
    }
}
