using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Compare.BaseOperations.Threshold
{
    public class ThresholdSwitchLogicFactory : LogicFactory
    {
        internal const double DefaultOn = 1;
        internal const double DefaultOff = 0;

        public static readonly Guid RuleInput = new Guid("8d4a2be4-0fba-464a-ba6f-7c91253e43ba");
        public static readonly Guid RuleParamOn = new Guid("4e04bbbd-8f66-4522-81af-8ca5c6d48bfa");
        public static readonly Guid RuleParamOff = new Guid("4a3f744a-f3fc-4f7b-8e12-9c4efd53d086");
        public static readonly Guid RuleOutput = new Guid("4120716b-f458-40cf-8362-8b43a4f2b169");

        public override string LogicName => "Compare.ThresholdSwitch";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("59be1aef-d1c2-474b-a916-63549e316e69");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.THRESHOLD.NAME", "COMPARE.THRESHOLD.DESCRIPTION", "compare.threshold", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "V", "COMPARE.THRESHOLD.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "Q", "COMPARE.THRESHOLD.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamOn, "COMPARE.THRESHOLD.ON.NAME", "COMPARE.THRESHOLD.ON.DESCRIPTION", "on", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultOn);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamOff, "COMPARE.THRESHOLD.OFF.NAME", "COMPARE.THRESHOLD.OFF.DESCRIPTION", "off", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultOff);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ThresholdSwitchRule(context);
        }
    }
}
