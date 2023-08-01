using Automatica.Core.Logic;

namespace Automatica.Core.WebApi.Tests.Logic
{
    public class TestLogic : Core.Logic.Logic
    {
        public TestLogic(ILogicContext context) : base(context)
        {
        }

        public override object GetDataForVisu()
        {
            return Context.RuleInstance.ObjId;
        }
    }
}
