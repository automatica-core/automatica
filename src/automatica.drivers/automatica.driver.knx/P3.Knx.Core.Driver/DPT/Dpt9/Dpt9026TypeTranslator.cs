namespace P3.Knx.Core.Driver.DPT.Dpt9
{
    internal class Dpt9026TypeTranslator : Dpt9Translator
    {
        public override string[] Ids => new[] { "9.026" };

        public override decimal ValidateMinMax(decimal value)
        {
            if (value < -671088.64m)
            {
                return -671088.64m;
            }
            if (value > 670760.96m)
            {
                return 670760.96m;
            }
            return value;
        }
    }
}
