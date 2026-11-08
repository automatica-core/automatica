using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.CommandRecognition
{
    /// <summary>
    /// Sends the extracted value when the text matches the pattern; texts that do not match keep the last value (like Loxone)
    /// </summary>
    public class CommandRecognitionRule : TextLogicBase
    {
        public CommandRecognitionRule(ILogicContext context) : base(context)
        {
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != CommandRecognitionLogicFactory.InputText || value == null)
            {
                return NoChanges();
            }

            var extracted = CommandPattern.Extract(ParameterText(CommandRecognitionLogicFactory.ParamPattern), TextValue.ToText(value),
                ParameterBool(CommandRecognitionLogicFactory.ParamSigned));
            if (extracted == null)
            {
                return NoChanges();
            }

            var change = Changed(CommandRecognitionLogicFactory.OutputValue, extracted.Value);
            return change == null ? NoChanges() : new List<ILogicOutputChanged> { change };
        }
    }
}
