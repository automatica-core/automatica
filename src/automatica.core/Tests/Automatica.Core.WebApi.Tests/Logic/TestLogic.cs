using Automatica.Core.Rule;

namespace Automatica.Core.WebApi.Tests.Logic
{
    public class TestLogic : Rule.Rule
    {
        public TestLogic(IRuleContext context) : base(context)
        {
        }

        public override object GetDataForVisu()
        {
            return Context.RuleInstance.ObjId;
        }
    }
}
