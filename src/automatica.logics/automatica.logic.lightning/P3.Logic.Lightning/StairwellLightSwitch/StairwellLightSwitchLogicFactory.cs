using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.StairwellLightSwitch
{
    /// <summary>
    /// Stairwell light switch (Loxone "Stairwell Light Switch"): Tr switches the light on for the on duration (every Tr restarts it),
    /// On switches it on permanently, Off switches it off; before the end the light blinks off once as a warning.
    /// </summary>
    public class StairwellLightSwitchLogicFactory : LogicFactory
    {
        internal const string Prefix = "LIGHTNING.STAIRWELL_LIGHT_SWITCH";

        public static readonly Guid Trigger = new("6b1f0c2e-4a7d-4c3b-9e15-2d8f7a6c1b01");

        public static readonly TimedSwitchIds Ids = new(
            new Guid("6b1f0c2e-4a7d-4c3b-9e15-2d8f7a6c1b02"),
            new Guid("6b1f0c2e-4a7d-4c3b-9e15-2d8f7a6c1b03"),
            new Guid("6b1f0c2e-4a7d-4c3b-9e15-2d8f7a6c1b04"),
            new Guid("6b1f0c2e-4a7d-4c3b-9e15-2d8f7a6c1b05"),
            new Guid("6b1f0c2e-4a7d-4c3b-9e15-2d8f7a6c1b06"),
            new Guid("6b1f0c2e-4a7d-4c3b-9e15-2d8f7a6c1b07"),
            PresenceSimulationIds.Derive("P3.Logic.Lightning.StairwellLightSwitch"));

        public override string LogicName => "StairwellLightSwitch";
        public override Guid LogicGuid => new("6b1f0c2e-4a7d-4c3b-9e15-2d8f7a6c1b00");
        public override Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{Prefix}.NAME", $"{Prefix}.DESCRIPTION", "lightning.stairwell_light_switch", "LIGHTNING.NAME", 100, 100);
            factory.CreateLogicInterfaceTemplate(Trigger, $"{Prefix}.INPUT.TRIGGER.NAME", $"{Prefix}.INPUT.TRIGGER.DESCRIPTION", "trigger", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            TimedSwitchLogic.CreateCommonTemplates(factory, LogicGuid, Prefix, Ids);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new StairwellLightSwitchLogic(context);
        }
    }
}
