using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.Multiplexer
{
    /// <summary>
    /// Passes the input selected by S (1..4) to the output. S out of range sends nothing.
    /// </summary>
    public class MultiplexerRule : Automatica.Core.Logic.Logic
    {
        private static readonly Guid[] Inputs =
        {
            MultiplexerLogicFactory.RuleInput1,
            MultiplexerLogicFactory.RuleInput2,
            MultiplexerLogicFactory.RuleInput3,
            MultiplexerLogicFactory.RuleInput4
        };

        private readonly RuleInterfaceInstance _output;
        private readonly object[] _values = new object[Inputs.Length];
        private int _selected;

        public MultiplexerRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == MultiplexerLogicFactory.RuleOutput);
        }

        internal static int ToSelector(object value)
        {
            switch (value)
            {
                case null:
                    return 0;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? (int)System.Math.Round(d) : 0;
                case IConvertible c:
                    try
                    {
                        var number = c.ToDouble(CultureInfo.InvariantCulture);
                        return double.IsNaN(number) ? 0 : (int)System.Math.Round(number);
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                default:
                    return 0;
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            if (template == MultiplexerLogicFactory.RuleSelect)
            {
                _selected = ToSelector(value);
            }
            else
            {
                var index = Array.IndexOf(Inputs, template);
                if (index < 0)
                {
                    return new List<ILogicOutputChanged>();
                }

                _values[index] = value;
                if (index + 1 != _selected)
                {
                    return new List<ILogicOutputChanged>();
                }
            }

            if (_selected < 1 || _selected > Inputs.Length || _values[_selected - 1] == null)
            {
                return new List<ILogicOutputChanged>();
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, _values[_selected - 1]));
        }
    }
}
