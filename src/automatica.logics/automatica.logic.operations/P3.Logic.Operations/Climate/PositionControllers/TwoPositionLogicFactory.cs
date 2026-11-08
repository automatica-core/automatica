using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;

namespace P3.Logic.Operations.Climate.PositionControllers
{
    public class TwoPositionLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("d21b345a-0eb1-4aff-b298-98d7a8010f28");

        public static readonly Guid InValue = ClimateTemplateBuilder.InputId(Id, "pv");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid ParamSetpoint = ClimateTemplateBuilder.ParamId(Id, "sp");
        public static readonly Guid ParamHysteresis = ClimateTemplateBuilder.ParamId(Id, "hys");
        public static readonly Guid ParamInverted = ClimateTemplateBuilder.ParamId(Id, "inv");
        public static readonly Guid OutOutput = ClimateTemplateBuilder.OutputId(Id, "o");

        public override string LogicName => "Climate.TwoPosition";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "TWO_POSITION", "climate.two-position");
            b.Input("pv", "PV", 1);
            b.Input("off", "Off", 2);
            b.Output("o", "O", 1);
            b.Number("sp", 1, 5, linkable: true);
            b.Number("hys", 2, 0.5);
            b.Bool("inv", 3, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new TwoPositionLogic(context);
    }

    /// <summary>
    /// Two-position (on/off) controller. Normal (heating): on below SP - Hys/2, off above SP + Hys/2, unchanged in between.
    /// Inverted (cooling): on above SP + Hys/2, off below SP - Hys/2. Off = 1 switches the output off and locks it.
    /// </summary>
    public class TwoPositionLogic : ClimateLogic
    {
        private bool _on;

        public TwoPositionLogic(ILogicContext context) : base(context)
        {
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            if (InBool(TwoPositionLogicFactory.InOff))
            {
                _on = false;
                outputs[TwoPositionLogicFactory.OutOutput] = false;
                return;
            }
            var pv = InNumber(TwoPositionLogicFactory.InValue);
            if (!pv.HasValue)
            {
                return;
            }
            var sp = Param(TwoPositionLogicFactory.ParamSetpoint, 5);
            var half = Math.Abs(Param(TwoPositionLogicFactory.ParamHysteresis, 0.5)) / 2;
            var inverted = ParamBool(TwoPositionLogicFactory.ParamInverted, false);

            if (!inverted)
            {
                if (pv.Value < sp - half)
                {
                    _on = true;
                }
                else if (pv.Value > sp + half)
                {
                    _on = false;
                }
            }
            else
            {
                if (pv.Value > sp + half)
                {
                    _on = true;
                }
                else if (pv.Value < sp - half)
                {
                    _on = false;
                }
            }
            outputs[TwoPositionLogicFactory.OutOutput] = _on;
        }
    }
}
