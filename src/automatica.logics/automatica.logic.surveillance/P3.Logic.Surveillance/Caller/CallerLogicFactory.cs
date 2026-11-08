using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Surveillance.Caller
{
    /// <summary>
    /// Call generator (Loxone "Call Generator" / "Caller"): calls phone numbers on a rising edge and reads a message with
    /// text to speech. The telephony provider is configurable (simulation or Twilio).
    /// </summary>
    public class CallerLogicFactory : LogicFactory
    {
        public const int ValueCount = 8;

        public static readonly Guid InTrigger = new Guid("6175e0b8-b1f8-4ad3-88d6-3ee688065e52");
        public static readonly Guid[] InValues =
        {
            new Guid("b10ec84f-1f19-45ba-9076-0d3261f9c18f"), new Guid("559380a6-30f6-48fc-8cac-9b987f8d81f7"), new Guid("c09dbfb1-94da-4e4c-956d-ae84e63a04a5"), new Guid("b25e4b19-51f0-4fd6-b610-9d0f15128293"),
            new Guid("fbeeadc6-2dae-4b8c-aa70-a1928076ca75"), new Guid("5ff5acd6-0168-4b94-bfc7-5f098a33fbba"), new Guid("ca5eeb56-1cb2-4f9a-88a9-7ec50711089c"), new Guid("25b15249-5d36-436a-be63-81f938a33312")
        };
        public static readonly Guid InOff = new Guid("f264a617-d79b-4a34-8af9-02cac2077d3f");

        public static readonly Guid OutCalls = new Guid("6c2e1df3-8c8b-448c-a909-0db0473e5241");
        public static readonly Guid OutResult = new Guid("7f48db05-b72d-4a48-9cdf-50d8505949fc");
        public static readonly Guid OutError = new Guid("40c0f7ae-f431-4344-b9de-353daa1606dd");

        public static readonly Guid ParamProvider = new Guid("326a2ec8-38a0-4b08-b1d4-17ee6cb61af6");
        public static readonly Guid ParamNumbers = new Guid("d2891d40-bf91-4308-a8ef-2ba9165ffdc0");
        public static readonly Guid ParamMessage = new Guid("1b3b1447-7805-4b7b-8bbb-083395d95142");
        public static readonly Guid ParamLanguage = new Guid("530968f8-23b4-4896-85dd-241f4cd270e0");
        public static readonly Guid ParamTriggerDelay = new Guid("7e7ddf64-3815-4a62-9e1d-8e4dca522312");
        public static readonly Guid ParamUpdateInterval = new Guid("b1996bb3-0fb0-4fab-b80a-b26b8a6ed8c8");
        public static readonly Guid ParamNotify = new Guid("b7b71bc4-5916-4e30-8911-2591a0511785");
        public static readonly Guid ParamTwilioSid = new Guid("67c7eaab-385e-4983-a380-f486f523c8a5");
        public static readonly Guid ParamTwilioToken = new Guid("cdadb959-1c0f-4d74-ae96-0244b2eb21f9");
        public static readonly Guid ParamTwilioFrom = new Guid("c3c8230d-ec3a-4f50-aa0d-c0480801056e");

        public override string LogicName => "Surveillance.Caller";
        public override Guid LogicGuid => new Guid("a71cd55c-95bc-49a3-95cb-2b47751a83d0");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string P = "SURVEILLANCE.CALLER.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{P}NAME", $"{P}DESCRIPTION", "surveillance.caller", "SURVEILLANCE.NAME", 100, 100);

            var sort = 1;
            factory.CreateLogicInterfaceTemplate(InTrigger, "Tr", $"{P}TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort++);
            for (var i = 0; i < ValueCount; i++)
            {
                factory.CreateLogicInterfaceTemplate(InValues[i], $"V{i + 1}", $"{P}VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort++);
            }
            factory.CreateLogicInterfaceTemplate(InOff, "Off", $"{P}OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort);

            factory.CreateLogicInterfaceTemplate(OutCalls, "Cnt", $"{P}CALLS.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutResult, "Res", $"{P}RESULT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutError, "Err", $"{P}ERROR.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 3, RuleInterfaceType.Status);

            sort = 1;
            void Param(Guid id, string key, RuleInterfaceParameterDataType type, object value) =>
                factory.CreateParameterLogicInterfaceTemplate(id, P + key + ".NAME", P + key + ".DESCRIPTION", LogicGuid, sort++, type, value);

            Param(ParamProvider, "PROVIDER", RuleInterfaceParameterDataType.Integer, 0);
            Param(ParamNumbers, "NUMBERS", RuleInterfaceParameterDataType.Text, "");
            Param(ParamMessage, "MESSAGE", RuleInterfaceParameterDataType.Text, CallerLogic.DefaultMessage);
            Param(ParamLanguage, "LANGUAGE", RuleInterfaceParameterDataType.Text, "de-DE");
            Param(ParamTriggerDelay, "TRIGGER_DELAY", RuleInterfaceParameterDataType.Integer, 0);
            Param(ParamUpdateInterval, "UPDATE_INTERVAL", RuleInterfaceParameterDataType.Integer, 0);
            Param(ParamNotify, "NOTIFY", RuleInterfaceParameterDataType.Bool, true);
            Param(ParamTwilioSid, "TWILIO_SID", RuleInterfaceParameterDataType.Text, "");
            Param(ParamTwilioToken, "TWILIO_TOKEN", RuleInterfaceParameterDataType.Text, "");
            Param(ParamTwilioFrom, "TWILIO_FROM", RuleInterfaceParameterDataType.Text, "");
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new CallerLogic(context, FileSecurityStateStore.Default, null);
        }
    }
}
