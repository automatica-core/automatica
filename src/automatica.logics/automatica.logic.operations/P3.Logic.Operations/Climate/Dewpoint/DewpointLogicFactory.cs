using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;

namespace P3.Logic.Operations.Climate.Dewpoint
{
    public class DewpointLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("d008a5de-ba5f-4b0b-9d8d-12e871d800cf");

        public static readonly Guid InTemperature = ClimateTemplateBuilder.InputId(Id, "t");
        public static readonly Guid InHumidity = ClimateTemplateBuilder.InputId(Id, "h");
        public static readonly Guid ParamOffset = ClimateTemplateBuilder.ParamId(Id, "o");
        public static readonly Guid OutDewpoint = ClimateTemplateBuilder.OutputId(Id, "td");

        public override string LogicName => "Climate.Dewpoint";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "DEWPOINT", "climate.dewpoint");
            b.Input("t", "T", 1);
            b.Input("h", "H", 2);
            b.Output("td", "Td", 1);
            b.Number("o", 1, 0);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new DewpointLogic(context);
    }

    /// <summary>
    /// Dew point of the air from temperature (°C) and relative humidity (%) with the Magnus formula, plus an offset.
    /// Nothing is sent outside -65..60 °C or without humidity.
    /// </summary>
    public class DewpointLogic : ClimateLogic
    {
        public DewpointLogic(ILogicContext context) : base(context)
        {
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var t = InNumber(DewpointLogicFactory.InTemperature);
            var h = InNumber(DewpointLogicFactory.InHumidity);
            if (!t.HasValue || !h.HasValue)
            {
                return;
            }
            var dew = HeatingCurveMath.DewPoint(t.Value, h.Value);
            if (dew.HasValue)
            {
                Set(outputs, DewpointLogicFactory.OutDewpoint, dew.Value + Param(DewpointLogicFactory.ParamOffset, 0));
            }
        }
    }
}
