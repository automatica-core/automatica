using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.WipingRelay
{
    public class EdgeWipingRelayLogicFactory : LogicFactory
    {
        internal const double DefaultOn = 1;
        internal const double DefaultOff = 2;
        internal const int DefaultCycles = 10;

        public static readonly Guid RuleTrigger = new Guid("6c49cf91-c49a-44c6-85e9-1b8b6891d1b7");
        public static readonly Guid RuleOff = new Guid("02844a17-3e99-433d-ba0f-448e24dbba31");
        public static readonly Guid RuleOutput = new Guid("b6faa997-2cc2-4ff2-86c4-388736001808");
        public static readonly Guid RuleParamOn = new Guid("c2850312-7631-4907-bff9-532d819311fe");
        public static readonly Guid RuleParamOff = new Guid("61988879-7aac-434b-bf65-5b4789b7b87d");
        public static readonly Guid RuleParamCycles = new Guid("d41c6820-ce3a-4b25-a177-ccd23bf1c8f9");

        public override string LogicName => "Time.EdgeWipingRelay";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("fe863f3e-2abe-4aa8-9775-6cbfbdd8b597");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.EDGE_WIPING_RELAY.NAME", "TIME.EDGE_WIPING_RELAY.DESCRIPTION", "time.edge_wiping_relay", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "Tr", "TIME.EDGE_WIPING_RELAY.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "TIME.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "P", "TIME.EDGE_WIPING_RELAY.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamOn, "TIME.EDGE_WIPING_RELAY.ON.NAME", "TIME.EDGE_WIPING_RELAY.ON.DESCRIPTION", "don", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultOn);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamOff, "TIME.EDGE_WIPING_RELAY.OFF.NAME", "TIME.EDGE_WIPING_RELAY.OFF.DESCRIPTION", "doff", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultOff);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamCycles, "TIME.EDGE_WIPING_RELAY.CYCLES.NAME", "TIME.EDGE_WIPING_RELAY.CYCLES.DESCRIPTION", "c", LogicGuid, 3, RuleInterfaceParameterDataType.Integer, DefaultCycles);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EdgeWipingRelayRule(context);
        }
    }
}
