using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using Polly;

namespace P3.Logic.Messenger
{
    public class MessengerLogic: Automatica.Core.Logic.Logic
    {
        private readonly IList<string> _to = new List<string>();
        private readonly string _subject = "Automatica.Core Message";

        private object _value;

        public MessengerLogic(ILogicContext context) : base(context)
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

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {

            if (_to.Count > 0 &&  value != null && value != _value && _value != null)
            {
                try
                {
                    var body = Context.LocalizationProvider.GetTranslation("MESSENGER.CLOUD_EMAIL.BODY");
                    body = body.Replace("{{NAME}}", Context.RuleInstance.Name).Replace("{{VALUE}}", value.ToString())
                        .Replace("{{SOURCE}}", source.Name);

                    Context.Logger.LogInformation($"Send email to {String.Join(";", _to)}");
                    Context.CloudApi.SendEmail(_to, _subject, body);

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
