using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Base;

namespace P3.Logic.Compare.BaseOperations.Smaller
{
    public class SmallerRule : BaseCompareRule
    {

        public SmallerRule(ILogicContext context) : base(context, SmallerLogicFactory.RuleInput1,
            SmallerLogicFactory.RuleInput2, SmallerLogicFactory.RuleOutput)
        {
        }

        protected override bool Compare(double i1, double i2)
        {
            return i1 < i2;
        }
    }
}
