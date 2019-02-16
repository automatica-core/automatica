using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Logic.Messenger
{
    public class MessengerLogic : Rule
    {
        private readonly IList<string> _to = new List<string>();
        private readonly string _subject = "Automatica.Core Message";

        public MessengerLogic(IRuleContext context) : base(context)
        {
            var toProperty = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MessengerFactory.ToProperty);

            var subjectProperty = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MessengerFactory.SubjectProperty);

            if (toProperty != null)
            {
                var toPropertySplit = toProperty.ValueString.Split(";");
                _to = toPropertySplit.ToList();
            }

            if (subjectProperty != null)
            {
                _subject = subjectProperty.ValueString;
            }
            
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (_to.Count > 0)
            {
                Context.CloudApi.SendEmail(_to, _subject, $"\"{Context.RuleInstance.Name}\" received value \"{value}\" from source \"{source.Name}\"");
            }
            
            return base.InputValueChanged(instance, source, value);
        }
    }
}
