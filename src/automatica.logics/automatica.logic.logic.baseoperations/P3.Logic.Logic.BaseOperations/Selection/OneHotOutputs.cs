using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.Selection
{
    /// <summary>
    /// A row of outputs of which at most one is 1, plus an output with the number of the active one (0 = none)
    /// </summary>
    internal sealed class OneHotOutputs
    {
        private readonly RuleInterfaceInstance[] _outputs;
        private readonly RuleInterfaceInstance _number;

        internal OneHotOutputs(ILogicContext context, IReadOnlyList<Guid> outputs, Guid number)
        {
            var interfaces = context.RuleInstance.RuleInterfaceInstance;
            _outputs = outputs.Select(id => interfaces.SingleOrDefault(a => a.This2RuleInterfaceTemplate == id)).ToArray();
            _number = interfaces.SingleOrDefault(a => a.This2RuleInterfaceTemplate == number);
        }

        internal int Count => _outputs.Length;

        internal int Selected { get; private set; }

        /// <summary>
        /// Activates output <paramref name="index"/> (1-based, 0 = all off); returns the changed outputs, nothing if it was active already
        /// </summary>
        internal IList<ILogicOutputChanged> Select(int index)
        {
            var changes = new List<ILogicOutputChanged>();
            if (index < 0 || index > _outputs.Length || index == Selected)
            {
                return changes;
            }

            if (Selected > 0)
            {
                changes.Add(new LogicOutputChanged(_outputs[Selected - 1], false));
            }
            if (index > 0)
            {
                changes.Add(new LogicOutputChanged(_outputs[index - 1], true));
            }

            Selected = index;
            changes.Add(new LogicOutputChanged(_number, index));
            return changes;
        }

        /// <summary>
        /// A number input as rounded integer, null if it is no number
        /// </summary>
        internal static int? ToIndex(object value)
        {
            double number;
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                    {
                        return null;
                    }
                    break;
                case IConvertible c:
                    try
                    {
                        number = c.ToDouble(CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    break;
                default:
                    return null;
            }

            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                return null;
            }

            return (int)System.Math.Round(System.Math.Clamp(number, int.MinValue, int.MaxValue));
        }
    }
}
