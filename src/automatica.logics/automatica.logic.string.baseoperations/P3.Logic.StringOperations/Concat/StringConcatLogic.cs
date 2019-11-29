using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Logic.StringOperations.Concat
{
    public class StringConcatLogic : Rule
    {
        public StringConcatLogic(IRuleContext context) : base(context)
        {
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            return base.InputValueChanged(instance, source, value);
        }
    }
}
