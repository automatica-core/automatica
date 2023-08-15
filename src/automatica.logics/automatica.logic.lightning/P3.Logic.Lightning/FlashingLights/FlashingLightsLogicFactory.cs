using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Lightning.FlashingLights
{
    public class FlashingLightsLogicFactory : LogicFactory
    {
        public override string LogicName => "FlashingLights";
        public override Guid LogicGuid => new Guid("2c55738a-221a-456b-8923-a6eead3de823");
        public override Version LogicVersion => new Version(0, 3, 0, 0);

        public static readonly Guid Trigger = new Guid("a5fd09a9-64f6-4eec-98a3-52740e30c9ac");
        public static readonly Guid State = new Guid("03fc863e-e657-4f83-afe1-fe29920f6615");

        public static readonly Guid Output = new Guid("57157462-715d-4d3d-92c2-7e373bdadcc7");

        public static readonly Guid Delay = new Guid("a320d9bc-0c8c-4e5b-93a3-b96546c426c5");
        public static readonly Guid RepeatCount = new Guid("5fde1d9b-6329-4ea3-9ec2-ff9f98f735c2");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LIGHTNING.FLASHING_LIGHTS.NAME", "LIGHTNING.FLASHING_LIGHTS.DESCRIPTION",
                "lightning.flashing_lights", "LIGHTNING.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(Trigger, "LIGHTNING.FLASHING_LIGHTS.TRIGGER.NAME", "LIGHTNING.FLASHING_LIGHTS.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(State, "LIGHTNING.FLASHING_LIGHTS.STATE.NAME", "LIGHTNING.FLASHING_LIGHTS.STATE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(Output, "LIGHTNING.FLASHING_LIGHTS.OUTPUT.NAME", "LIGHTNING.FLASHING_LIGHTS.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 1, 3);

            factory.CreateParameterLogicInterfaceTemplate(Delay, "LIGHTNING.FLASHING_LIGHTS.DELAY.NAME",
                "LIGHTNING.FLASHING_LIGHTS.DELAY.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Integer,
                1000, false);
            factory.CreateParameterLogicInterfaceTemplate(RepeatCount, "LIGHTNING.FLASHING_LIGHTS.REPEAT_COUNT.NAME",
                "LIGHTNING.FLASHING_LIGHTS.REPEAT_COUNT.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Integer,
                2, false);

        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new FlashingLightsLogic(context);
        }

    }
}
