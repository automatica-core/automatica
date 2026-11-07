using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Math.BasicOperations.Formula
{
    /// <summary>
    /// Calculates a free formula over I1..I4 (see <see cref="FormulaExpression"/>). Inputs without a value count as 0.
    /// An invalid formula or a result that is not a finite number sends nothing.
    /// </summary>
    public class FormulaRule : Automatica.Core.Logic.Logic
    {
        private static readonly Guid[] Inputs =
        {
            FormulaLogicFactory.RuleInput1,
            FormulaLogicFactory.RuleInput2,
            FormulaLogicFactory.RuleInput3,
            FormulaLogicFactory.RuleInput4
        };

        private readonly RuleInterfaceInstance _output;
        private readonly double[] _values = new double[FormulaExpression.VariableCount];
        private FormulaExpression _formula;

        public FormulaRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == FormulaLogicFactory.RuleOutput);
            SetFormula(FormulaLogicFactory.DefaultFormula);
        }

        internal FormulaExpression Formula => _formula;

        private void SetFormula(string text)
        {
            try
            {
                _formula = FormulaExpression.Parse(text);
            }
            catch (FormatException e)
            {
                _formula = null;
                Context.Logger.LogError($"{Context.RuleInstance.Name}: {e.Message}");
            }
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == FormulaLogicFactory.RuleParamFormula)
            {
                SetFormula(value?.ToString());
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var index = Array.IndexOf(Inputs, instance.This2RuleInterfaceTemplate);
            if (index < 0)
            {
                return new List<ILogicOutputChanged>();
            }

            _values[index] = MathValue.ToDouble(value) ?? 0;

            if (_formula == null)
            {
                return new List<ILogicOutputChanged>();
            }

            var result = _formula.Evaluate(_values);
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                Context.Logger.LogWarning($"{Context.RuleInstance.Name}: {_formula.Text} is not a number for the current inputs");
                return new List<ILogicOutputChanged>();
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, result));
        }
    }
}
