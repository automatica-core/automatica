using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.StringOperations.PulseBy
{
    /// <summary>
    /// Pulse by (Loxone "Pulse by"): when the text on T contains one of up to four search patterns (wildcards * and ?),
    /// P is 1 for the pulse duration Pd.
    /// </summary>
    public class PulseByLogicFactory : LogicFactory
    {
        internal const int PatternCount = 4;
        internal const double DefaultDuration = 1;

        public static readonly Guid InputText = new Guid("82c5aee9-f392-4186-94b3-d94a5a6e1201");
        public static readonly Guid OutputPulse = new Guid("93d6bffa-04a3-4297-a5c4-ea5b6b7f1301");
        public static readonly Guid ParamDuration = new Guid("a4e7c00b-15b4-43a8-b6d5-fb6c7c801401");
        public static readonly Guid ParamIgnoreCase = new Guid("a4e7c00b-15b4-43a8-b6d5-fb6c7c801402");
        public static readonly Guid[] ParamPatterns =
        {
            new Guid("a4e7c00b-15b4-43a8-b6d5-fb6c7c801411"),
            new Guid("a4e7c00b-15b4-43a8-b6d5-fb6c7c801412"),
            new Guid("a4e7c00b-15b4-43a8-b6d5-fb6c7c801413"),
            new Guid("a4e7c00b-15b4-43a8-b6d5-fb6c7c801414")
        };

        public override string LogicName => "StringOperations.PulseBy";
        public override Guid LogicGuid => new Guid("df61a25c-8e72-4a4f-9b19-62c34e507186");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new PulseByRule(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "STRING_OPERATIONS.PULSE_BY.NAME", "STRING_OPERATIONS.PULSE_BY.DESCRIPTION",
                "string_operations.pulse_by", "STRING_OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputText, "T", "STRING_OPERATIONS.PULSE_BY.INPUT.TEXT.DESCRIPTION", "text",
                LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(OutputPulse, "P", "STRING_OPERATIONS.PULSE_BY.OUTPUT.PULSE.DESCRIPTION", "pulse",
                LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            var sort = 0;
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration, "STRING_OPERATIONS.PULSE_BY.PARAM.DURATION.NAME",
                "STRING_OPERATIONS.PULSE_BY.PARAM.DURATION.DESCRIPTION", "duration", LogicGuid, ++sort, RuleInterfaceParameterDataType.Double, DefaultDuration, false);
            for (var i = 0; i < ParamPatterns.Length; i++)
            {
                factory.CreateParameterLogicInterfaceTemplate(ParamPatterns[i], $"STRING_OPERATIONS.PULSE_BY.PARAM.PATTERN{i + 1}.NAME",
                    "STRING_OPERATIONS.PULSE_BY.PARAM.PATTERN.DESCRIPTION", $"pattern{i + 1}", LogicGuid, ++sort, RuleInterfaceParameterDataType.Text, "", false);
            }
            factory.CreateParameterLogicInterfaceTemplate(ParamIgnoreCase, "STRING_OPERATIONS.PULSE_BY.PARAM.IGNORE_CASE.NAME",
                "STRING_OPERATIONS.PULSE_BY.PARAM.IGNORE_CASE.DESCRIPTION", "ignoreCase", LogicGuid, ++sort, RuleInterfaceParameterDataType.Bool, false, false);
        }
    }
}
