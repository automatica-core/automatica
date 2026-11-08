using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Lightning.RgbLightingController
{
    /// <summary>
    /// RGB lighting controller (Loxone "RGB Lighting Controller"): colour scenes picked with +/-, a scene number or a direct colour,
    /// outputs per channel (0..100 %), combined in the Loxone format (BBBGGGRRR) and as "#RRGGBB".
    /// </summary>
    public class RgbLightingControllerLogicFactory : LogicFactory
    {
        internal const string Prefix = "LIGHTNING.RGB_LIGHTING_CONTROLLER";
        internal const string DefaultScenes = "#FFFFFF;#FFB060;#FF0000;#00FF00;#0000FF";

        public static readonly Guid Color = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d01");
        public static readonly Guid Plus = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d02");
        public static readonly Guid Minus = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d03");
        public static readonly Guid Scene = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d04");
        public static readonly Guid Disable = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d05");
        public static readonly Guid Reset = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d06");
        public static readonly Guid AllOn = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d07");

        public static readonly Guid Red = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d11");
        public static readonly Guid Green = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d12");
        public static readonly Guid Blue = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d13");
        public static readonly Guid ActiveScene = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d14");
        public static readonly Guid Combined = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d15");
        public static readonly Guid Hex = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d16");

        public static readonly Guid Scenes = new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d21");

        public override string LogicName => "RgbLightingController";
        public override Guid LogicGuid => new("8d3b2e40-6c9f-4e5d-a037-4fab9c8e3d00");
        public override Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{Prefix}.NAME", $"{Prefix}.DESCRIPTION", "lightning.rgb_lighting_controller", "LIGHTNING.NAME", 150, 100);

            var sort = 0;
            void RgbInput(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, $"{Prefix}.INPUT.{name}.NAME", $"{Prefix}.INPUT.{name}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Input, 0, ++sort);
            RgbInput(Color, "COLOR", "color");
            RgbInput(Plus, "PLUS", "plus");
            RgbInput(Minus, "MINUS", "minus");
            RgbInput(Scene, "SCENE", "scene");
            RgbInput(Disable, "DISABLE", "disable");
            RgbInput(Reset, "RESET", "reset");
            RgbInput(AllOn, "ALL_ON", "allOn");

            sort = 0;
            void RgbOutput(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, $"{Prefix}.OUTPUT.{name}.NAME", $"{Prefix}.OUTPUT.{name}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Output, 0, ++sort, RuleInterfaceType.Output);
            RgbOutput(Red, "RED", "red");
            RgbOutput(Green, "GREEN", "green");
            RgbOutput(Blue, "BLUE", "blue");
            RgbOutput(ActiveScene, "SCENE", "activeScene");
            RgbOutput(Combined, "COMBINED", "combined");
            RgbOutput(Hex, "HEX", "hex");

            factory.CreateParameterLogicInterfaceTemplate(Scenes, $"{Prefix}.PARAM.SCENES.NAME", $"{Prefix}.PARAM.SCENES.DESCRIPTION", "scenes", LogicGuid, 1, RuleInterfaceParameterDataType.Text, DefaultScenes, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new RgbLightingControllerLogic(context);
        }
    }
}
