using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.StringOperations.CommandRecognition
{
    /// <summary>
    /// Command recognition (Loxone "Command Recognition"): extracts a value from the text on T with a search pattern
    /// (e.g. <c>\iTemp: \i\v</c>), see <see cref="CommandPattern"/>.
    /// </summary>
    public class CommandRecognitionLogicFactory : LogicFactory
    {
        public static readonly Guid InputText = new Guid("5f927bb6-c06f-4e53-a180-a61727381001");
        public static readonly Guid OutputValue = new Guid("60a38cc7-d170-4f64-b291-b72838491101");
        public static readonly Guid ParamPattern = new Guid("71b49dd8-e281-4075-83a2-c8394950a201");
        public static readonly Guid ParamSigned = new Guid("71b49dd8-e281-4075-83a2-c8394950a202");

        public override string LogicName => "StringOperations.CommandRecognition";
        public override Guid LogicGuid => new Guid("ce50914b-7d61-4f3e-8a08-51b23d4f6075");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new CommandRecognitionRule(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "STRING_OPERATIONS.COMMAND_RECOGNITION.NAME", "STRING_OPERATIONS.COMMAND_RECOGNITION.DESCRIPTION",
                "string_operations.command_recognition", "STRING_OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputText, "T", "STRING_OPERATIONS.COMMAND_RECOGNITION.INPUT.TEXT.DESCRIPTION", "text",
                LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(OutputValue, "Lv", "STRING_OPERATIONS.COMMAND_RECOGNITION.OUTPUT.VALUE.DESCRIPTION", "value",
                LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(ParamPattern, "STRING_OPERATIONS.COMMAND_RECOGNITION.PARAM.PATTERN.NAME",
                "STRING_OPERATIONS.COMMAND_RECOGNITION.PARAM.PATTERN.DESCRIPTION", "pattern", LogicGuid, 1, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamSigned, "STRING_OPERATIONS.COMMAND_RECOGNITION.PARAM.SIGNED.NAME",
                "STRING_OPERATIONS.COMMAND_RECOGNITION.PARAM.SIGNED.DESCRIPTION", "signed", LogicGuid, 2, RuleInterfaceParameterDataType.Bool, false, false);
        }
    }
}
