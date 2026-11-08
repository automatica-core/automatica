using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.LightingCentral
{
    /// <summary>
    /// Lighting central (Loxone "Lighting Central"): drives many lighting controllers together. Its outputs are linked to the same
    /// inputs of every lighting controller (+, -, mood, off, on, disable motion); the "active" outputs of the controllers are linked
    /// back to count the rooms with light (Na).
    /// Instead of linking, the rooms can be picked in the "lights" parameter (lighting controllers, switches and dimmers, the
    /// same picker as the Matter bridge): they get the commands directly and are counted without a link.
    /// </summary>
    public class LightingCentralLogicFactory : LogicFactory
    {
        internal const string Prefix = "LIGHTNING.LIGHTING_CENTRAL";
        public const int Rooms = 8;

        public static readonly Guid MoodPlus = new("a05d4062-8eb1-4a7f-8259-61cdbea05f01");
        public static readonly Guid MoodMinus = new("a05d4062-8eb1-4a7f-8259-61cdbea05f02");
        public static readonly Guid Mood = new("a05d4062-8eb1-4a7f-8259-61cdbea05f03");
        public static readonly Guid Off = new("a05d4062-8eb1-4a7f-8259-61cdbea05f04");
        public static readonly Guid On = new("a05d4062-8eb1-4a7f-8259-61cdbea05f05");
        public static readonly Guid DisableMotion = new("a05d4062-8eb1-4a7f-8259-61cdbea05f06");
        public static readonly Guid[] Active = TemplateIds.Series("P3.Logic.Lightning.LightingCentral.Active", Rooms);

        public static readonly Guid MoodPlusOut = new("a05d4062-8eb1-4a7f-8259-61cdbea05f11");
        public static readonly Guid MoodMinusOut = new("a05d4062-8eb1-4a7f-8259-61cdbea05f12");
        public static readonly Guid MoodOut = new("a05d4062-8eb1-4a7f-8259-61cdbea05f13");
        public static readonly Guid OffOut = new("a05d4062-8eb1-4a7f-8259-61cdbea05f14");
        public static readonly Guid OnOut = new("a05d4062-8eb1-4a7f-8259-61cdbea05f15");
        public static readonly Guid DisableMotionOut = new("a05d4062-8eb1-4a7f-8259-61cdbea05f16");
        public static readonly Guid ActiveCount = new("a05d4062-8eb1-4a7f-8259-61cdbea05f17");
        public static readonly Guid AnyActive = new("a05d4062-8eb1-4a7f-8259-61cdbea05f18");

        public static readonly Guid ParamLights = new("a05d4062-8eb1-4a7f-8259-61cdbea05f21");

        public override string LogicName => "LightingCentral";
        public override Guid LogicGuid => new("a05d4062-8eb1-4a7f-8259-61cdbea05f00");
        // 1.1: "lights" parameter (pick the rooms instead of linking them)
        public override Version LogicVersion => new(1, 1, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{Prefix}.NAME", $"{Prefix}.DESCRIPTION", "lightning.lighting_central", "LIGHTNING.NAME", 150, 100);

            var sort = 0;
            void CentralInput(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, $"{Prefix}.INPUT.{name}.NAME", $"{Prefix}.INPUT.{name}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Input, 0, ++sort);
            CentralInput(MoodPlus, "MOOD_PLUS", "moodPlus");
            CentralInput(MoodMinus, "MOOD_MINUS", "moodMinus");
            CentralInput(Mood, "MOOD", "mood");
            CentralInput(Off, "OFF", "off");
            CentralInput(On, "ON", "on");
            CentralInput(DisableMotion, "DISABLE_MOTION", "disableMotion");
            for (var i = 0; i < Rooms; i++)
            {
                factory.CreateLogicInterfaceTemplate(Active[i], $"{Prefix}.INPUT.ACTIVE{i + 1}.NAME", $"{Prefix}.INPUT.ACTIVE.DESCRIPTION", $"active{i + 1}", LogicGuid, LogicInterfaceDirection.Input, 0, ++sort);
            }

            sort = 0;
            void CentralOutput(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, $"{Prefix}.OUTPUT.{name}.NAME", $"{Prefix}.OUTPUT.{name}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Output, 0, ++sort, RuleInterfaceType.Output);
            CentralOutput(MoodPlusOut, "MOOD_PLUS", "moodPlusOut");
            CentralOutput(MoodMinusOut, "MOOD_MINUS", "moodMinusOut");
            CentralOutput(MoodOut, "MOOD", "moodOut");
            CentralOutput(OffOut, "OFF", "offOut");
            CentralOutput(OnOut, "ON", "onOut");
            CentralOutput(DisableMotionOut, "DISABLE_MOTION", "disableMotionOut");
            CentralOutput(ActiveCount, "ACTIVE_COUNT", "activeCount");
            CentralOutput(AnyActive, "ANY_ACTIVE", "anyActive");

            factory.CreateParameterLogicInterfaceTemplate(ParamLights, $"{Prefix}.PARAM.LIGHTS.NAME", $"{Prefix}.PARAM.LIGHTS.DESCRIPTION", "lights", LogicGuid, 1,
                RuleInterfaceParameterDataType.Controls, null);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new LightingCentralLogic(context);
        }
    }
}
