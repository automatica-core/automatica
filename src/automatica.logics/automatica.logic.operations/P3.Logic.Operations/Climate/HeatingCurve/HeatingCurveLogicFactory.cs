using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;

namespace P3.Logic.Operations.Climate.HeatingCurve
{
    public class HeatingCurveLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("2d842265-2a72-47cb-94a9-294e699a58f9");

        internal const double DefaultSetpoint = 20;

        public static readonly Guid InTarget = ClimateTemplateBuilder.InputId(Id, "tt");
        public static readonly Guid InOutdoor = ClimateTemplateBuilder.InputId(Id, "ct");
        public static readonly Guid InDisable = ClimateTemplateBuilder.InputId(Id, "dis");
        public static readonly Guid ParamSlope = ClimateTemplateBuilder.ParamId(Id, "s");
        public static readonly Guid ParamOffset = ClimateTemplateBuilder.ParamId(Id, "o");
        public static readonly Guid ParamMin = ClimateTemplateBuilder.ParamId(Id, "minft");
        public static readonly Guid ParamMax = ClimateTemplateBuilder.ParamId(Id, "maxft");
        public static readonly Guid OutFlow = ClimateTemplateBuilder.OutputId(Id, "ft");
        public static readonly Guid OutInvalid = ClimateTemplateBuilder.OutputId(Id, "iv");

        public override string LogicName => "Climate.HeatingCurve";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "HEATING_CURVE", "climate.heating-curve");
            b.Input("tt", "Tt", 1);
            b.Input("ct", "Ct", 2);
            b.Input("dis", "Dis", 3);
            b.Output("ft", "Ft", 1);
            b.Output("iv", "Iv", 2);
            b.Number("s", 1, 1.3);
            b.Number("o", 2, 0);
            b.Number("minft", 3, 15);
            b.Number("maxft", 4, 65);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new HeatingCurveLogic(context);
    }

    /// <summary>
    /// Weather compensated flow temperature from room setpoint (Tt, 20 °C when unlinked or disabled by Dis) and outdoor temperature (Ct).
    /// Ft is limited to minFt..maxFt, Iv = 1 while the curve is outside these limits.
    /// </summary>
    public class HeatingCurveLogic : ClimateLogic
    {
        public HeatingCurveLogic(ILogicContext context) : base(context)
        {
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var outdoor = InNumber(HeatingCurveLogicFactory.InOutdoor);
            if (!outdoor.HasValue)
            {
                return;
            }
            var setpoint = InBool(HeatingCurveLogicFactory.InDisable)
                ? HeatingCurveLogicFactory.DefaultSetpoint
                : InNumber(HeatingCurveLogicFactory.InTarget) ?? HeatingCurveLogicFactory.DefaultSetpoint;

            var flow = HeatingCurveMath.Heating(setpoint, outdoor.Value, Param(HeatingCurveLogicFactory.ParamSlope, 1.3), Param(HeatingCurveLogicFactory.ParamOffset, 0));
            var min = Param(HeatingCurveLogicFactory.ParamMin, 15);
            var max = Param(HeatingCurveLogicFactory.ParamMax, 65);
            var limited = Clamp(flow, min, max);

            Set(outputs, HeatingCurveLogicFactory.OutFlow, limited, 1);
            outputs[HeatingCurveLogicFactory.OutInvalid] = Math.Abs(limited - flow) > 1e-9;
        }
    }
}
