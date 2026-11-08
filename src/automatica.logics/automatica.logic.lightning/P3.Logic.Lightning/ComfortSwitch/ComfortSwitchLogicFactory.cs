using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.ComfortSwitch
{
    /// <summary>
    /// Comfort switch (Loxone "Comfort Switch"): a short press on Tg switches the light on for the on duration or off again,
    /// a long press (at least the long-click time) switches it on permanently; on and off inputs, switch-off pre-warning.
    /// </summary>
    public class ComfortSwitchLogicFactory : LogicFactory
    {
        internal const string Prefix = "LIGHTNING.COMFORT_SWITCH";
        /// <summary>milliseconds</summary>
        internal const int DefaultLongClickMs = 500;

        public static readonly Guid Trigger = new("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c01");
        public static readonly Guid LongClick = new("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c08");

        public static readonly TimedSwitchIds Ids = new(
            new Guid("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c02"),
            new Guid("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c03"),
            new Guid("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c04"),
            new Guid("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c05"),
            new Guid("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c06"),
            new Guid("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c07"),
            PresenceSimulationIds.Derive("P3.Logic.Lightning.ComfortSwitch"));

        public override string LogicName => "ComfortSwitch";
        public override Guid LogicGuid => new("7c2a1d3f-5b8e-4d4c-8f26-3e9a8b7d2c00");
        public override Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{Prefix}.NAME", $"{Prefix}.DESCRIPTION", "lightning.comfort_switch", "LIGHTNING.NAME", 100, 100);
            factory.CreateLogicInterfaceTemplate(Trigger, $"{Prefix}.INPUT.TRIGGER.NAME", $"{Prefix}.INPUT.TRIGGER.DESCRIPTION", "trigger", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            TimedSwitchLogic.CreateCommonTemplates(factory, LogicGuid, Prefix, Ids);
            factory.CreateParameterLogicInterfaceTemplate(LongClick, $"{Prefix}.PARAM.LONG_CLICK.NAME", $"{Prefix}.PARAM.LONG_CLICK.DESCRIPTION", "longClick", LogicGuid, 2, RuleInterfaceParameterDataType.Integer, DefaultLongClickMs, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ComfortSwitchLogic(context);
        }
    }
}
