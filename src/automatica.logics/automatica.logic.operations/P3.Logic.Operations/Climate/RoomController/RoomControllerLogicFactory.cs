using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;

namespace P3.Logic.Operations.Climate.RoomController
{
    public class RoomControllerLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("98d62f85-951a-4acd-b2ac-260770068267");

        /// <summary>
        /// VisuMobileObjectTemplateTypes.RoomController - added to the core enum after the core package this plugin builds against
        /// </summary>

        public static readonly Guid InMode = ClimateTemplateBuilder.InputId(Id, "mode");
        public static readonly Guid InTarget = ClimateTemplateBuilder.InputId(Id, "tt");
        public static readonly Guid InCurrent = ClimateTemplateBuilder.InputId(Id, "tc");
        public static readonly Guid InWindow = ClimateTemplateBuilder.InputId(Id, "dwc");
        public static readonly Guid InComfort = ClimateTemplateBuilder.InputId(Id, "comfort");
        public static readonly Guid InEco = ClimateTemplateBuilder.InputId(Id, "eco");
        public static readonly Guid InBuildingProtection = ClimateTemplateBuilder.InputId(Id, "bp");
        public static readonly Guid InPresence = ClimateTemplateBuilder.InputId(Id, "p");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid InDisablePresence = ClimateTemplateBuilder.InputId(Id, "disp");
        public static readonly Guid InOutdoor = ClimateTemplateBuilder.InputId(Id, "to");
        public static readonly Guid InComfortTemperature = ClimateTemplateBuilder.InputId(Id, "tcomf");
        public static readonly Guid InSourceMode = ClimateTemplateBuilder.InputId(Id, "sm");
        public static readonly Guid InExcessHeating = ClimateTemplateBuilder.InputId(Id, "eh");
        public static readonly Guid InExcessCooling = ClimateTemplateBuilder.InputId(Id, "ec");

        public static readonly Guid OutHeating = ClimateTemplateBuilder.OutputId(Id, "h");
        public static readonly Guid OutCooling = ClimateTemplateBuilder.OutputId(Id, "cool");
        public static readonly Guid OutHeatingCooling = ClimateTemplateBuilder.OutputId(Id, "hc");
        public static readonly Guid OutDemand = ClimateTemplateBuilder.OutputId(Id, "demand");
        public static readonly Guid OutShading = ClimateTemplateBuilder.OutputId(Id, "shd");
        public static readonly Guid OutHeatingCoolingMode = ClimateTemplateBuilder.OutputId(Id, "hcm");
        public static readonly Guid OutError = ClimateTemplateBuilder.OutputId(Id, "error");
        public static readonly Guid OutTarget = ClimateTemplateBuilder.OutputId(Id, "target");
        public static readonly Guid OutBoost = ClimateTemplateBuilder.OutputId(Id, "boost");
        public static readonly Guid OutTemperatureMode = ClimateTemplateBuilder.OutputId(Id, "os");
        public static readonly Guid OutComfortHeating = ClimateTemplateBuilder.OutputId(Id, "tchout");
        public static readonly Guid OutComfortCooling = ClimateTemplateBuilder.OutputId(Id, "tccout");

        public static readonly Guid ParamComfortHeating = ClimateTemplateBuilder.ParamId(Id, "tch");
        public static readonly Guid ParamComfortCooling = ClimateTemplateBuilder.ParamId(Id, "tcc");
        public static readonly Guid ParamEcoHeating = ClimateTemplateBuilder.ParamId(Id, "teh");
        public static readonly Guid ParamEcoCooling = ClimateTemplateBuilder.ParamId(Id, "tec");
        public static readonly Guid ParamFrostProtection = ClimateTemplateBuilder.ParamId(Id, "tfp");
        public static readonly Guid ParamHeatProtection = ClimateTemplateBuilder.ParamId(Id, "thp");
        public static readonly Guid ParamShadingHeating = ClimateTemplateBuilder.ParamId(Id, "tsh");
        public static readonly Guid ParamShadingCooling = ClimateTemplateBuilder.ParamId(Id, "tsc");
        public static readonly Guid ParamDeviation = ClimateTemplateBuilder.ParamId(Id, "td");
        public static readonly Guid ParamExcessOffset = ClimateTemplateBuilder.ParamId(Id, "texc");
        public static readonly Guid ParamComfortExtend = ClimateTemplateBuilder.ParamId(Id, "cet");
        public static readonly Guid ParamEcoExtend = ClimateTemplateBuilder.ParamId(Id, "ebpet");
        public static readonly Guid ParamPresenceExtend = ClimateTemplateBuilder.ParamId(Id, "pet");
        public static readonly Guid ParamWindowDelay = ClimateTemplateBuilder.ParamId(Id, "ddwc");
        public static readonly Guid ParamBand = ClimateTemplateBuilder.ParamId(Id, "pb");
        public static readonly Guid ParamKi = ClimateTemplateBuilder.ParamId(Id, "ki");
        public static readonly Guid ParamDefaultMode = ClimateTemplateBuilder.ParamId(Id, "default");

        internal const double DefaultComfortHeating = 22;
        internal const double DefaultComfortCooling = 24.5;
        internal const double DefaultEco = 3;
        internal const double DefaultFrost = 5;
        internal const double DefaultHeatProtection = 28;
        internal const double DefaultShadingHeating = 27.5;
        internal const double DefaultShadingCooling = 23.5;
        internal const double DefaultDeviation = 1.5;
        internal const double DefaultExcess = 1;
        internal const double DefaultComfortExtend = 3600;
        internal const double DefaultEcoExtend = 3600;
        internal const double DefaultPresenceExtend = 1800;
        internal const double DefaultWindowDelay = 300;
        internal const double DefaultBand = 2;
        internal const double DefaultKi = 0.01;

        public override string LogicName => "Climate.RoomController";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "ROOM_CONTROLLER", "climate.room-controller", 200, 100);
            var i = 1;
            b.Input("mode", "Mode", i++);
            b.Input("tt", "Tt", i++);
            b.Input("tc", "Tc", i++);
            b.Input("dwc", "Dwc", i++);
            b.Input("comfort", "C", i++);
            b.Input("eco", "E", i++);
            b.Input("bp", "Bp", i++);
            b.Input("p", "P", i++);
            b.Input("off", "Off", i++);
            b.Input("disp", "DisP", i++);
            b.Input("to", "To", i++);
            b.Input("tcomf", "Tcomf", i++);
            b.Input("sm", "Sm", i++);
            b.Input("eh", "Eh", i++);
            b.Input("ec", "Ec", i);

            var o = 1;
            b.Output("h", "H", o++);
            b.Output("cool", "C", o++);
            b.Output("hc", "HC", o++);
            b.Output("demand", "D", o++);
            b.Output("shd", "Shd", o++);
            b.Output("hcm", "HCm", o++);
            b.Output("error", "Error", o++);
            b.Output("target", "Tt", o++);
            b.Output("boost", "Boost", o++);
            b.Output("os", "Os", o++);
            b.Output("tchout", "Tch", o++);
            b.Output("tccout", "Tcc", o);

            var p = 1;
            b.Number("tch", p++, DefaultComfortHeating);
            b.Number("tcc", p++, DefaultComfortCooling);
            b.Number("teh", p++, DefaultEco);
            b.Number("tec", p++, DefaultEco);
            b.Number("tfp", p++, DefaultFrost);
            b.Number("thp", p++, DefaultHeatProtection);
            b.Number("tsh", p++, DefaultShadingHeating);
            b.Number("tsc", p++, DefaultShadingCooling);
            b.Number("td", p++, DefaultDeviation);
            b.Number("texc", p++, DefaultExcess);
            b.Number("cet", p++, DefaultComfortExtend);
            b.Number("ebpet", p++, DefaultEcoExtend);
            b.Number("pet", p++, DefaultPresenceExtend);
            b.Number("ddwc", p++, DefaultWindowDelay);
            b.Number("pb", p++, DefaultBand);
            b.Number("ki", p++, DefaultKi);
            b.Enum("default", p, 1, (0, "ECO"), (1, "COMFORT"));

            b.VisuTile(ClimateTile.RoomController);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new RoomControllerLogic(context);
    }
}
