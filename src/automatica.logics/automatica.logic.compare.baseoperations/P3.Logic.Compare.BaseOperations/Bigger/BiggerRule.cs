using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Base;

namespace P3.Logic.Compare.BaseOperations.Bigger
{
    public class BiggerRule : BaseCompareRule
    {

        public BiggerRule(ILogicContext context) : base(context, BiggerLogicFactory.RuleInput1, BiggerLogicFactory.RuleInput2, BiggerLogicFactory.RuleOutput)
        {
        }

        protected override bool Compare(double i1, double i2)
        {
            return i1 > i2;
        }
    }
}
