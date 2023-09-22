using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Base;

namespace P3.Logic.Compare.BaseOperations.SmallerOrEqual
{
    public class SmallerOrEqualRule : BaseCompareRule
    {
      
        public SmallerOrEqualRule(ILogicContext context) : base(context, SmallerOrEqualLogicFactory.RuleInput1, SmallerOrEqualLogicFactory.RuleInput2, SmallerOrEqualLogicFactory.RuleOutput)
        {
        }

        protected override bool Compare(double i1, double i2)
        {
            return i1 <= i2;
        }
    }
}
