using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Lightning.ConstantBrightness
{
    /// <summary>
    /// Constant brightness controller (Loxone "Constant Brightness Controller"): while Act is 1 the dim level is moved in steps until
    /// the measured brightness is within the hysteresis around the target; Set forces a level, Off switches off.
    /// </summary>
    public class ConstantBrightnessLogicFactory : LogicFactory
    {
        internal const string Prefix = "LIGHTNING.CONSTANT_BRIGHTNESS";
        internal const double DefaultTarget = 500;
        internal const double DefaultHysteresis = 10;
        internal const double DefaultStep = 5;
        internal const double DefaultInterval = 5;

        public static readonly Guid Brightness = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e01");
        public static readonly Guid Set = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e02");
        public static readonly Guid Off = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e03");
        public static readonly Guid Active = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e04");

        public static readonly Guid Level = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e11");

        public static readonly Guid Target = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e21");
        public static readonly Guid Hysteresis = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e22");
        public static readonly Guid Step = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e23");
        public static readonly Guid Interval = new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e24");

        public override string LogicName => "ConstantBrightness";
        public override Guid LogicGuid => new("9e4c3f51-7da0-4f6e-b148-50bcad9f4e00");
        public override Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{Prefix}.NAME", $"{Prefix}.DESCRIPTION", "lightning.constant_brightness", "LIGHTNING.NAME", 100, 100);

            var sort = 0;
            void BrightnessInput(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, $"{Prefix}.INPUT.{name}.NAME", $"{Prefix}.INPUT.{name}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Input, 0, ++sort);
            BrightnessInput(Brightness, "BRIGHTNESS", "brightness");
            BrightnessInput(Set, "SET", "set");
            BrightnessInput(Off, "OFF", "off");
            BrightnessInput(Active, "ACTIVE", "active");

            factory.CreateLogicInterfaceTemplate(Level, $"{Prefix}.OUTPUT.LEVEL.NAME", $"{Prefix}.OUTPUT.LEVEL.DESCRIPTION", "level", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);

            void BrightnessParam(Guid id, string name, string key, int order, double value) =>
                factory.CreateParameterLogicInterfaceTemplate(id, $"{Prefix}.PARAM.{name}.NAME", $"{Prefix}.PARAM.{name}.DESCRIPTION", key, LogicGuid, order, RuleInterfaceParameterDataType.Double, value, false);
            BrightnessParam(Target, "TARGET", "target", 1, DefaultTarget);
            BrightnessParam(Hysteresis, "HYSTERESIS", "hysteresis", 2, DefaultHysteresis);
            BrightnessParam(Step, "STEP", "step", 3, DefaultStep);
            BrightnessParam(Interval, "INTERVAL", "interval", 4, DefaultInterval);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ConstantBrightnessLogic(context);
        }
    }
}
