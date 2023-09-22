using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Base;

namespace P3.Logic.Compare.BaseOperations.Equal
{
    public class EqualRule : BaseCompareRule
    {
        public EqualRule(ILogicContext context) : base(context, EqualLogicFactory.RuleInput1, EqualLogicFactory.RuleInput2, EqualLogicFactory.RuleOutput)
        {
         
        }
        protected override bool Compare(double i1, double i2)
        {
            return i1 == i2;
        }
    }
}
