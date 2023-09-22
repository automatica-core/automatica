using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Base;

namespace P3.Logic.Compare.BaseOperations.BiggerOrEqual
{
    public class BiggerOrEqualRule : BaseCompareRule
    {
        public BiggerOrEqualRule(ILogicContext context) : base(context, BiggerOrEqualLogicFactory.RuleInput1, BiggerOrEqualLogicFactory.RuleInput2, BiggerOrEqualLogicFactory.RuleOutput)
        {
        }

        protected override bool Compare(double i1, double i2)
        {
            return i1 >= i2;
        }
    }
}
