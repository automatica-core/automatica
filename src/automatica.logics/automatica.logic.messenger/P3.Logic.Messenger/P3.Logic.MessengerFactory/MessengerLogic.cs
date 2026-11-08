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
        private IList<string> _to = new List<string>();
        private string _subject = "Automatica.Core Message";
        private string _bodyTemplate;

        private object _value;

        public MessengerLogic(ILogicContext context) : base(context)
        {
            var toProperty = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MessengerFactory.ToProperty);

            var subjectProperty = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MessengerFactory.SubjectProperty);

            if (toProperty != null)
            {
                _to = ParseRecipients(toProperty.ValueString);
            }

            if (subjectProperty != null)
            {
                _subject = subjectProperty.ValueString;
            }

            var bodyProperty = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MessengerFactory.BodyProperty);
            _bodyTemplate = bodyProperty?.ValueString;
            
        }

        private static IList<string> ParseRecipients(string to)
        {
            return (to ?? String.Empty).Split(";").Select(a => a.Trim()).Where(a => a.Length > 0).ToList();
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            // parameters are pushed again on start and reload (same object), so the constructor values can be stale
            if (instance.This2RuleInterfaceTemplate == MessengerFactory.ToProperty)
            {
                _to = ParseRecipients(value?.ToString());
            }
            else if (instance.This2RuleInterfaceTemplate == MessengerFactory.SubjectProperty)
            {
                _subject = value?.ToString();
            }
            else if (instance.This2RuleInterfaceTemplate == MessengerFactory.BodyProperty)
            {
                _bodyTemplate = value?.ToString();
            }
            base.ParameterValueChanged(instance, source, value);
        }

        private string Render(string template, object value, IDispatchable source)
        {
            var now = Context.TimeProvider?.GetLocalNow() ?? DateTimeOffset.Now;
            return (template ?? String.Empty)
                .Replace("{{NAME}}", Context.RuleInstance.Name)
                .Replace("{{VALUE}}", value?.ToString() ?? String.Empty)
                .Replace("{{SOURCE}}", source?.Name ?? String.Empty)
                .Replace("{{TIMESTAMP}}", now.ToString("F"))
                .Replace("{{DATE}}", now.ToString("d"))
                .Replace("{{TIME}}", now.ToString("t"));
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {

            // Equals: the values are boxed, != would compare references
            if (_to.Count > 0 && value != null && !Equals(value, _value))
            {
                try
                {
                    // own text (mail generator) or the default text of the language
                    var template = String.IsNullOrWhiteSpace(_bodyTemplate)
                        ? Context.LocalizationProvider.GetTranslation("MESSENGER.CLOUD_EMAIL.BODY")
                        : _bodyTemplate;
                    if (template == null)
                    {
                        Context.Logger.LogWarning("No mail text (translation MESSENGER.CLOUD_EMAIL.BODY missing), not sent");
                        _value = value;
                        return base.InputValueChanged(instance, source, value);
                    }
                    var body = Render(template, value, source);
                    var subject = String.IsNullOrWhiteSpace(_subject) ? "Automatica.Core Message" : Render(_subject, value, source);

                    // the core sends through its own SMTP server when it is configured, otherwise through the cloud
                    Context.Logger.LogInformation($"Send email to {String.Join(";", _to)}");
                    Context.CloudApi.SendEmail(_to, subject, body);

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
