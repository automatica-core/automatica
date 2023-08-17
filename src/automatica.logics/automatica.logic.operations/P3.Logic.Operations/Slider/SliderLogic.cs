using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using NuGet.Packaging;

namespace P3.Logic.Operations.Slider
{
    public class SliderData
    {
        public double? LastValue { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
    }

    public class SliderLogic: Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _value;

        private readonly RuleInterfaceInstance _valueMax;
        private readonly RuleInterfaceInstance _valueMin;

        private readonly RuleInterfaceInstance _reset;
        private readonly RuleInterfaceInstance _output;
        private readonly RuleInterfaceInstance _stateOutput;

        private double? _lastValue;
        private bool? _lastOutputState;

        private double? _min;
        private double? _max;

        public SliderLogic(ILogicContext context) : base(context)
        {
         
            _value = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == SliderLogicFactory.RuleInputValue);
 
            _reset = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == SliderLogicFactory.RuleInputReset);

            _output = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == SliderLogicFactory.RuleOutput);
            _stateOutput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == SliderLogicFactory.RuleStateOutput);

            _valueMin = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == SliderLogicFactory.RuleInputValueMin);
            _valueMax = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == SliderLogicFactory.RuleInputValueMax);
        }

        public override Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            _min = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == SliderLogicFactory.RuleInputValueMinParam)!.ValueDouble;

            _max = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == SliderLogicFactory.RuleInputValueMaxParam)!.ValueDouble;

            return base.Start(token);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            if (instance.ObjId == _value.ObjId)
            {
                var dValue = Convert.ToDouble(value);

                if (_lastValue.HasValue && _lastValue.Value == dValue)
                {
                    return new List<ILogicOutputChanged>();
                }

                if (_min.HasValue && dValue < _min.Value)
                {
                    dValue = _min.Value;
                }

                if (_max.HasValue && dValue > _max.Value)
                {
                    dValue = _max.Value;
                }

                _lastValue = dValue;
                _lastOutputState = dValue > 0;

                var outputs = SingleOutputChanged(new LogicOutputChanged(_output, _lastValue.Value));
                outputs.AddRange(SingleOutputChanged(new LogicOutputChanged(_stateOutput, _lastOutputState.Value)));

                return outputs;
            }

            if (instance.ObjId == _reset.ObjId)
            {
                return SingleOutputChanged(new LogicOutputChanged(_output, 0));
            }

            if (instance.ObjId == _valueMin.ObjId)
            {
                _min = Convert.ToDouble(value);
            }
            if (instance.ObjId == _valueMax.ObjId)
            {
                _max = Convert.ToDouble(value);
            }
            return new List<ILogicOutputChanged>();
        }

        public override object GetDataForVisu()
        {
            return new SliderData
            {
                LastValue = _lastValue,
                MinValue = _min,
                MaxValue = _max
            };
        }
    }
}
