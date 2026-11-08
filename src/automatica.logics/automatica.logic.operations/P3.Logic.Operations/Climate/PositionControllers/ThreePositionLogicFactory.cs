using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;

namespace P3.Logic.Operations.Climate.PositionControllers
{
    public class ThreePositionLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("0253b2a9-74e3-4a24-aa3e-7585bac39ffa");

        public static readonly Guid InValue = ClimateTemplateBuilder.InputId(Id, "pv");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid ParamSetpoint1 = ClimateTemplateBuilder.ParamId(Id, "sp1");
        public static readonly Guid ParamSetpoint2 = ClimateTemplateBuilder.ParamId(Id, "sp2");
        public static readonly Guid ParamHysteresis = ClimateTemplateBuilder.ParamId(Id, "hys");
        public static readonly Guid OutOutput1 = ClimateTemplateBuilder.OutputId(Id, "o1");
        public static readonly Guid OutOutput2 = ClimateTemplateBuilder.OutputId(Id, "o2");

        public override string LogicName => "Climate.ThreePosition";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "THREE_POSITION", "climate.three-position");
            b.Input("pv", "PV", 1);
            b.Input("off", "Off", 2);
            b.Output("o1", "O1", 1);
            b.Output("o2", "O2", 2);
            b.Number("sp1", 1, 3, linkable: true);
            b.Number("sp2", 2, 7, linkable: true);
            b.Number("hys", 3, 0);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new ThreePositionLogic(context);
    }

    /// <summary>
    /// Three-position controller with two outputs of opposite effect: O1 below SP1 (e.g. heat / open), O2 above SP2 (e.g. cool / close),
    /// both off between the setpoints. An output switches off again once the value is Hys back inside the band (Hys = 0: at the setpoint).
    /// The setpoints are sorted, SP1 is always the lower one. Off = 1 switches both outputs off.
    /// </summary>
    public class ThreePositionLogic : ClimateLogic
    {
        private bool _o1;
        private bool _o2;

        public ThreePositionLogic(ILogicContext context) : base(context)
        {
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            if (InBool(ThreePositionLogicFactory.InOff))
            {
                _o1 = _o2 = false;
                outputs[ThreePositionLogicFactory.OutOutput1] = false;
                outputs[ThreePositionLogicFactory.OutOutput2] = false;
                return;
            }
            var pv = InNumber(ThreePositionLogicFactory.InValue);
            if (!pv.HasValue)
            {
                return;
            }
            var a = Param(ThreePositionLogicFactory.ParamSetpoint1, 3);
            var b = Param(ThreePositionLogicFactory.ParamSetpoint2, 7);
            var sp1 = Math.Min(a, b);
            var sp2 = Math.Max(a, b);
            var hys = Math.Abs(Param(ThreePositionLogicFactory.ParamHysteresis, 0));

            if (pv.Value < sp1)
            {
                _o1 = true;
            }
            else if (pv.Value >= sp1 + hys)
            {
                _o1 = false;
            }

            if (pv.Value > sp2)
            {
                _o2 = true;
            }
            else if (pv.Value <= sp2 - hys)
            {
                _o2 = false;
            }

            outputs[ThreePositionLogicFactory.OutOutput1] = _o1;
            outputs[ThreePositionLogicFactory.OutOutput2] = _o2;
        }
    }
}
