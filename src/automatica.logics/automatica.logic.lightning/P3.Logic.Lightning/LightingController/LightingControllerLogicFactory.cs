using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.LightingController
{
    /// <summary>
    /// Lighting controller of a room (Loxone "Lighting Controller"): up to 8 circuits (switch, dimmer, RGB, tunable white),
    /// moods that are picked with +/-, a mood number or the visualization and saved from the visualization, motion with a
    /// brightness threshold and presence simulation.
    /// </summary>
    public class LightingControllerLogicFactory : LogicFactory
    {
        public const int Circuits = LightingControllerIds.Circuits;
        internal const string Prefix = "LIGHTNING.LIGHTING_CONTROLLER";

        /// <summary>the visu template of the core (VisuMobileObjectTemplateTypes.LightingController), looked up by name so the plugin builds against older core packages</summary>
        internal const string VisuTemplate = "LightingController";

        /// <summary>Loxone Moet: overrun after motion ends</summary>
        internal const double DefaultMotionOffDelay = 900;
        /// <summary>Loxone Met: overrun after motion ends when the lights were operated by hand (0 = never off)</summary>
        internal const double DefaultManualOffDelay = 3600;
        /// <summary>Loxone Pto: after switching off by hand motion stays ignored until there was no motion for this long</summary>
        internal const double DefaultAutomaticTimeout = 300;
        /// <summary>Loxone Brt (lux), 0 = no threshold</summary>
        internal const double DefaultBrightnessThreshold = 30;

        internal const string CircuitTypeMeta = "ENUM(0,LIGHTNING.CIRCUIT_TYPE.UNUSED;1,LIGHTNING.CIRCUIT_TYPE.SWITCH;2,LIGHTNING.CIRCUIT_TYPE.DIMMER;3,LIGHTNING.CIRCUIT_TYPE.RGB;4,LIGHTNING.CIRCUIT_TYPE.TUNABLE_WHITE)";

        public static readonly LightingControllerIds Ids = LightingControllerIds.Derive("P3.Logic.Lightning.LightingController");

        public override string LogicName => "LightingController";
        public override Guid LogicGuid => new("1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c01");
        public override Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{Prefix}.NAME", $"{Prefix}.DESCRIPTION", "lightning.lighting_controller", "LIGHTNING.NAME", 200, 100);
            CreateControllerInterfaces(factory, LogicGuid, Ids);
            UseVisu(factory, LogicGuid);
        }

        /// <summary>
        /// The interfaces of the lighting controller, also used by the hotel lighting controller (inputs sort 1..9, outputs 1..19, params 1..20, presence simulation 100+)
        /// </summary>
        internal static void CreateControllerInterfaces(ILogicTemplateFactory factory, Guid logicGuid, LightingControllerIds ids)
        {
            var sort = 0;
            void ControllerInput(Guid id, string name, string inputKey) =>
                factory.CreateLogicInterfaceTemplate(id, $"{Prefix}.INPUT.{name}.NAME", $"{Prefix}.INPUT.{name}.DESCRIPTION", inputKey, logicGuid, LogicInterfaceDirection.Input, 0, ++sort);

            ControllerInput(ids.MoodPlus, "MOOD_PLUS", "moodPlus");
            ControllerInput(ids.MoodMinus, "MOOD_MINUS", "moodMinus");
            ControllerInput(ids.On, "ON", "on");
            ControllerInput(ids.Off, "OFF", "off");
            ControllerInput(ids.MoodSelect, "MOOD_SELECT", "moodSelect");
            ControllerInput(ids.Motion, "MOTION", "motion");
            ControllerInput(ids.Brightness, "BRIGHTNESS", "brightness");
            ControllerInput(ids.DisableMotion, "DISABLE_MOTION", "disableMotion");
            ControllerInput(ids.Command, "COMMAND", "command");

            sort = 0;
            for (var i = 0; i < Circuits; i++)
            {
                factory.CreateLogicInterfaceTemplate(ids.CircuitOutputs[i], $"{Prefix}.OUTPUT.CIRCUIT{i + 1}.NAME", $"{Prefix}.OUTPUT.CIRCUIT.DESCRIPTION",
                    $"circuit{i + 1}", logicGuid, LogicInterfaceDirection.Output, 0, ++sort, RuleInterfaceType.Output);
            }
            for (var i = 0; i < Circuits; i++)
            {
                factory.CreateLogicInterfaceTemplate(ids.KelvinOutputs[i], $"{Prefix}.OUTPUT.KELVIN{i + 1}.NAME", $"{Prefix}.OUTPUT.KELVIN.DESCRIPTION",
                    $"kelvin{i + 1}", logicGuid, LogicInterfaceDirection.Output, 0, ++sort, RuleInterfaceType.Output);
            }
            factory.CreateLogicInterfaceTemplate(ids.ActiveMood, $"{Prefix}.OUTPUT.MOOD.NAME", $"{Prefix}.OUTPUT.MOOD.DESCRIPTION", "mood", logicGuid, LogicInterfaceDirection.Output, 0, ++sort, RuleInterfaceType.Output);
            factory.CreateLogicInterfaceTemplate(ids.AnyOn, $"{Prefix}.OUTPUT.ACTIVE.NAME", $"{Prefix}.OUTPUT.ACTIVE.DESCRIPTION", "active", logicGuid, LogicInterfaceDirection.Output, 0, ++sort, RuleInterfaceType.Output);
            factory.CreateLogicInterfaceTemplate(ids.VisuState, $"{Prefix}.OUTPUT.VISU_STATE.NAME", $"{Prefix}.OUTPUT.VISU_STATE.DESCRIPTION", "visuState", logicGuid, LogicInterfaceDirection.Output, 0, ++sort, RuleInterfaceType.Output);

            var param = 0;
            for (var i = 0; i < Circuits; i++)
            {
                factory.CreateParameterLogicInterfaceTemplate(ids.CircuitTypes[i], $"{Prefix}.PARAM.CIRCUIT{i + 1}_TYPE.NAME", $"{Prefix}.PARAM.CIRCUIT_TYPE.DESCRIPTION",
                    $"circuit{i + 1}Type", logicGuid, ++param, RuleInterfaceParameterDataType.Enum, i == 0 ? (int)CircuitType.Dimmer : (int)CircuitType.Unused, false, CircuitTypeMeta);
                factory.CreateParameterLogicInterfaceTemplate(ids.CircuitNames[i], $"{Prefix}.PARAM.CIRCUIT{i + 1}_NAME.NAME", $"{Prefix}.PARAM.CIRCUIT_NAME.DESCRIPTION",
                    $"circuit{i + 1}Name", logicGuid, ++param, RuleInterfaceParameterDataType.Text, "", false);
            }

            factory.CreateParameterLogicInterfaceTemplate(ids.Moods, $"{Prefix}.PARAM.MOODS.NAME", $"{Prefix}.PARAM.MOODS.DESCRIPTION", "moods", logicGuid, ++param, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ids.MotionMood, $"{Prefix}.PARAM.MOTION_MOOD.NAME", $"{Prefix}.PARAM.MOTION_MOOD.DESCRIPTION", "motionMood", logicGuid, ++param, RuleInterfaceParameterDataType.Integer, Mood.AllOn, false);
            factory.CreateParameterLogicInterfaceTemplate(ids.MotionOffDelay, $"{Prefix}.PARAM.MOTION_OFF_DELAY.NAME", $"{Prefix}.PARAM.MOTION_OFF_DELAY.DESCRIPTION", "motionOffDelay", logicGuid, ++param, RuleInterfaceParameterDataType.Double, DefaultMotionOffDelay, false);
            factory.CreateParameterLogicInterfaceTemplate(ids.ManualOffDelay, $"{Prefix}.PARAM.MANUAL_OFF_DELAY.NAME", $"{Prefix}.PARAM.MANUAL_OFF_DELAY.DESCRIPTION", "manualOffDelay", logicGuid, ++param, RuleInterfaceParameterDataType.Double, DefaultManualOffDelay, false);
            factory.CreateParameterLogicInterfaceTemplate(ids.AutomaticTimeout, $"{Prefix}.PARAM.AUTOMATIC_TIMEOUT.NAME", $"{Prefix}.PARAM.AUTOMATIC_TIMEOUT.DESCRIPTION", "automaticTimeout", logicGuid, ++param, RuleInterfaceParameterDataType.Double, DefaultAutomaticTimeout, false);
            factory.CreateParameterLogicInterfaceTemplate(ids.BrightnessThreshold, $"{Prefix}.PARAM.BRIGHTNESS_THRESHOLD.NAME", $"{Prefix}.PARAM.BRIGHTNESS_THRESHOLD.DESCRIPTION", "brightnessThreshold", logicGuid, ++param, RuleInterfaceParameterDataType.Double, DefaultBrightnessThreshold, false);

            PresenceSimulationBinding.CreateTemplates(factory, logicGuid, ids.PresenceSimulation, 100, 100);
        }

        /// <summary>sets the lighting controller tile as default visu template, false when the core does not know it yet</summary>
        internal static bool UseVisu(ILogicTemplateFactory factory, Guid logicGuid)
        {
            if (!Enum.TryParse(VisuTemplate, false, out VisuMobileObjectTemplateTypes template)
                || !Enum.IsDefined(typeof(VisuMobileObjectTemplateTypes), template))
            {
                return false;
            }
            factory.ChangeDefaultVisuTemplate(logicGuid, template);
            return true;
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new LightingControllerLogic(context, Ids);
        }
    }
}
