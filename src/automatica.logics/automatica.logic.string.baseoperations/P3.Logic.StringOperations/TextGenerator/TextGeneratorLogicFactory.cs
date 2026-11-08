using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.StringOperations.TextGenerator
{
    /// <summary>
    /// Text generator (Loxone "Text Generator"): five text templates with the placeholders &lt;v1&gt;..&lt;v8&gt; (&lt;v1.2&gt; = 2 decimals)
    /// for the inputs V1..V8. A rising edge on Tr generates the texts (after the trigger delay Td), the falling edge clears them;
    /// while Tr stays on they are generated again every Tu seconds.
    /// </summary>
    public class TextGeneratorLogicFactory : LogicFactory
    {
        internal const int TextCount = 5;
        internal const int ValueCount = 8;

        public static readonly Guid InputTrigger = new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081401");
        public static readonly Guid[] InputValues =
        {
            new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081411"),
            new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081412"),
            new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081413"),
            new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081414"),
            new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081415"),
            new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081416"),
            new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081417"),
            new Guid("2c6f4883-9d3c-4b20-9f5d-73e4f4081418")
        };

        public static readonly Guid[] Outputs =
        {
            new Guid("3d705994-ae4d-4c31-806e-84f505190501"),
            new Guid("3d705994-ae4d-4c31-806e-84f505190502"),
            new Guid("3d705994-ae4d-4c31-806e-84f505190503"),
            new Guid("3d705994-ae4d-4c31-806e-84f505190504"),
            new Guid("3d705994-ae4d-4c31-806e-84f505190505")
        };

        public static readonly Guid ParamDelay = new Guid("4e816aa5-bf5e-4d42-917f-950616290601");
        public static readonly Guid ParamInterval = new Guid("4e816aa5-bf5e-4d42-917f-950616290602");
        public static readonly Guid[] ParamTexts =
        {
            new Guid("4e816aa5-bf5e-4d42-917f-950616290611"),
            new Guid("4e816aa5-bf5e-4d42-917f-950616290612"),
            new Guid("4e816aa5-bf5e-4d42-917f-950616290613"),
            new Guid("4e816aa5-bf5e-4d42-917f-950616290614"),
            new Guid("4e816aa5-bf5e-4d42-917f-950616290615")
        };

        public override string LogicName => "StringOperations.TextGenerator";
        public override Guid LogicGuid => new Guid("bd4f803a-6c50-4e2d-99f7-40a12c3e5f64");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new TextGeneratorRule(context);
        }

        /// <summary>output / text names: Txt, Txt1..Txt4</summary>
        internal static string TextName(int index) => index == 0 ? "Txt" : $"Txt{index}";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "STRING_OPERATIONS.TEXT_GENERATOR.NAME", "STRING_OPERATIONS.TEXT_GENERATOR.DESCRIPTION",
                "string_operations.text_generator", "STRING_OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputTrigger, "Tr", "STRING_OPERATIONS.TEXT_GENERATOR.INPUT.TRIGGER.DESCRIPTION", "trigger",
                LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            for (var i = 0; i < InputValues.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(InputValues[i], $"V{i + 1}", "STRING_OPERATIONS.TEXT_GENERATOR.INPUT.VALUE.DESCRIPTION", $"v{i + 1}",
                    LogicGuid, LogicInterfaceDirection.Input, 1, i + 2);
            }

            for (var i = 0; i < Outputs.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(Outputs[i], TextName(i), "STRING_OPERATIONS.TEXT_GENERATOR.OUTPUT.TEXT.DESCRIPTION", TextName(i).ToLowerInvariant(),
                    LogicGuid, LogicInterfaceDirection.Output, 0, i + 1, i == 0 ? RuleInterfaceType.Status : RuleInterfaceType.Output);
            }

            var sort = 0;
            factory.CreateParameterLogicInterfaceTemplate(ParamDelay, "STRING_OPERATIONS.TEXT_GENERATOR.PARAM.DELAY.NAME",
                "STRING_OPERATIONS.TEXT_GENERATOR.PARAM.DELAY.DESCRIPTION", "delay", LogicGuid, ++sort, RuleInterfaceParameterDataType.Integer, 0, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamInterval, "STRING_OPERATIONS.TEXT_GENERATOR.PARAM.INTERVAL.NAME",
                "STRING_OPERATIONS.TEXT_GENERATOR.PARAM.INTERVAL.DESCRIPTION", "interval", LogicGuid, ++sort, RuleInterfaceParameterDataType.Integer, 0, false);
            for (var i = 0; i < ParamTexts.Length; i++)
            {
                factory.CreateParameterLogicInterfaceTemplate(ParamTexts[i], $"STRING_OPERATIONS.TEXT_GENERATOR.PARAM.TEXT{i}.NAME",
                    "STRING_OPERATIONS.TEXT_GENERATOR.PARAM.TEXT.DESCRIPTION", $"text{i}", LogicGuid, ++sort, RuleInterfaceParameterDataType.Text, "", false);
            }
        }
    }
}
