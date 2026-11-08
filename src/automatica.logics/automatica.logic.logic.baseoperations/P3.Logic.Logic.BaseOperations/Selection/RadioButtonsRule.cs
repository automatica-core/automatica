using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.Selection
{
    /// <summary>
    /// What the visualisation gets: the slider reads min/max, <see cref="Labels"/> names the outputs
    /// </summary>
    public class RadioButtonsData
    {
        public double? LastValue { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public string[] Labels { get; set; }
    }

    /// <summary>
    /// Radio buttons (Loxone "Radio Buttons"), 16 outputs of which at most one is 1; N is the number of the active one (0 = all off).
    /// A 1 on Ix activates Ox, + / - step to the next / previous output (wrapping over "all off" unless Skip 0 is set),
    /// Sel activates an output by number. These inputs only reach outputs 1..Max and are ignored while DisPc is 1.
    /// The visualisation writes the UI input; it can activate every output regardless of Max and DisPc.
    /// Off = 1 switches all outputs off and locks the block until Off is 0 again.
    /// </summary>
    public class RadioButtonsRule : Automatica.Core.Logic.Logic
    {
        private readonly OneHotOutputs _outputs;
        private readonly Dictionary<Guid, int> _inputIndex;

        private int _max = RadioButtonsLogicFactory.Size;
        private bool _skipZero;
        private bool _disabled;
        private bool _locked;
        private string[] _labels = Array.Empty<string>();

        public RadioButtonsRule(ILogicContext context) : base(context)
        {
            _outputs = new OneHotOutputs(context, RadioButtonsLogicFactory.RuleOutputs, RadioButtonsLogicFactory.RuleNumber);
            _inputIndex = RadioButtonsLogicFactory.RuleInputs.Select((id, i) => (id, i)).ToDictionary(a => a.id, a => a.i + 1);
        }

        internal static string[] ParseLabels(object value)
        {
            var text = value as string ?? value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            var labels = text.Split(';').Select(a => a.Trim()).Take(RadioButtonsLogicFactory.Size).ToArray();
            // trailing empty entries do not count as labelled outputs
            var count = labels.Length;
            while (count > 0 && labels[count - 1].Length == 0)
            {
                count--;
            }
            return labels.Take(count).ToArray();
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            if (template == RadioButtonsLogicFactory.RuleParamMax)
            {
                _max = System.Math.Clamp(OneHotOutputs.ToIndex(value) ?? RadioButtonsLogicFactory.Size, 1, RadioButtonsLogicFactory.Size);
            }
            else if (template == RadioButtonsLogicFactory.RuleParamSkipZero)
            {
                _skipZero = Helper.ConvertValueToBool(value);
            }
            else if (template == RadioButtonsLogicFactory.RuleParamLabels)
            {
                _labels = ParseLabels(value);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        public override object GetDataForVisu()
        {
            return new RadioButtonsData
            {
                LastValue = _outputs.Selected,
                MinValue = 0,
                // the visualisation may select every labelled output, without labels the outputs up to Max
                MaxValue = _labels.Length > 0 ? _labels.Length : _max,
                Labels = _labels
            };
        }

        private int Next()
        {
            var next = _outputs.Selected + 1;
            if (next > _max)
            {
                next = _skipZero ? 1 : 0;
            }
            return next;
        }

        private int Previous()
        {
            var previous = System.Math.Min(_outputs.Selected, _max + 1) - 1;
            if (previous < 0 || (previous == 0 && _skipZero))
            {
                previous = _max;
            }
            return previous;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            var none = new List<ILogicOutputChanged>();

            if (template == RadioButtonsLogicFactory.RuleOff)
            {
                _locked = Helper.ConvertValueToBool(value);
                return _locked ? _outputs.Select(0) : none;
            }
            if (template == RadioButtonsLogicFactory.RuleDisable)
            {
                _disabled = Helper.ConvertValueToBool(value);
                return none;
            }
            if (_locked)
            {
                return none;
            }

            if (template == RadioButtonsLogicFactory.RuleVisu)
            {
                var index = OneHotOutputs.ToIndex(value);
                return index.HasValue ? _outputs.Select(index.Value) : none;
            }
            if (_disabled)
            {
                return none;
            }

            if (_inputIndex.TryGetValue(template, out var input))
            {
                return Helper.ConvertValueToBool(value) && input <= _max ? _outputs.Select(input) : none;
            }
            if (template == RadioButtonsLogicFactory.RuleNext)
            {
                return Helper.ConvertValueToBool(value) ? _outputs.Select(Next()) : none;
            }
            if (template == RadioButtonsLogicFactory.RulePrevious)
            {
                return Helper.ConvertValueToBool(value) ? _outputs.Select(Previous()) : none;
            }
            if (template == RadioButtonsLogicFactory.RuleSelect)
            {
                var index = OneHotOutputs.ToIndex(value);
                return index.HasValue && index.Value >= 0 && index.Value <= _max ? _outputs.Select(index.Value) : none;
            }

            return none;
        }
    }
}
