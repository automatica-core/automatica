using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.MinMax
{
    /// <summary>
    /// Minimum, maximum and average of the inputs that have a value (unlinked inputs do not count as 0).
    /// </summary>
    public class MinMaxRule : Automatica.Core.Logic.Logic
    {
        private static readonly Guid[] Inputs =
        {
            MinMaxLogicFactory.RuleInput1,
            MinMaxLogicFactory.RuleInput2,
            MinMaxLogicFactory.RuleInput3,
            MinMaxLogicFactory.RuleInput4
        };

        private readonly RuleInterfaceInstance _min;
        private readonly RuleInterfaceInstance _max;
        private readonly RuleInterfaceInstance _average;
        private readonly double?[] _values = new double?[Inputs.Length];

        public MinMaxRule(ILogicContext context) : base(context)
        {
            _min = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == MinMaxLogicFactory.RuleMin);
            _max = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == MinMaxLogicFactory.RuleMax);
            _average = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == MinMaxLogicFactory.RuleAverage);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var index = Array.IndexOf(Inputs, instance.This2RuleInterfaceTemplate);
            if (index < 0)
            {
                return new List<ILogicOutputChanged>();
            }

            _values[index] = MathValue.ToDouble(value);

            var known = _values.Where(a => a.HasValue).Select(a => a.Value).ToArray();
            if (known.Length == 0)
            {
                return new List<ILogicOutputChanged>();
            }

            return new List<ILogicOutputChanged>
            {
                new LogicOutputChanged(_min, known.Min()),
                new LogicOutputChanged(_max, known.Max()),
                new LogicOutputChanged(_average, known.Average())
            };
        }
    }
}
