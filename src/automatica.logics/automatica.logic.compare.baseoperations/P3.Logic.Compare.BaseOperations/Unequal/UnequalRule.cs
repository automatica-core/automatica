using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Base;

namespace P3.Logic.Compare.BaseOperations.Unequal
{
    public class UnequalRule : BaseCompareRule
    {

        public UnequalRule(ILogicContext context) : base(context, UnequalLogicFactory.RuleInput1, UnequalLogicFactory.RuleInput2, UnequalLogicFactory.RuleOutput)
        {
        }

        protected override bool Compare(double i1, double i2)
        {
            return i1 != i2;
        }
    }
}
