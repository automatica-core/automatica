using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Messenger
{
    public class MessengerLogic : Rule
    {
        private readonly IList<string> _to = new List<string>();
        private readonly string _subject = "Automatica.Core Message";

        private object _value;

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

            if (_to.Count > 0 && value != _value)
            {
                try
                {
                    bool execute = false;
                    if (value is bool bValue)
                        execute = bValue;
                    else
                        execute = true;

                    if (execute)
                    {
                        Context.Logger.LogInformation($"Send email to {String.Join(";", _to)}");
                        Context.CloudApi.SendEmail(_to, _subject,
                            $"\"{Context.RuleInstance.Name}\" received value \"{value}\" from source \"{source.Name}\"");
                    }
                    else
                    {
                        Context.Logger.LogInformation($"Ignore message, because value is either false or hasn't changed newValue {value} oldValue {_value}");
                    }
                }
                catch (Exception e)
                {
                    Context.Logger.LogError(e, $"Error sending email {e}");
                }
            }

            _value = value;
            return base.InputValueChanged(instance, source, value);
        }
    }
}
