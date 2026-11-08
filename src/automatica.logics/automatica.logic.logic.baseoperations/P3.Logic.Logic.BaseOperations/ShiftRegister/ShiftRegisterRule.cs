using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Logic.BaseOperations.Selection;

namespace P3.Logic.Logic.BaseOperations.ShiftRegister
{
    /// <summary>
    /// Shift register (Loxone "Shift Register") with 64 bits.
    /// Every 1 on Tr shifts the register by one bit and stores D at the entry: Dir = 0 shifts up and enters at bit 1,
    /// Dir = 1 shifts down and enters at bit 64. O is the bit selected by the register bit parameter, so with Dir = 0
    /// a value on D shows up on O after that many pulses.
    /// </summary>
    public class ShiftRegisterRule : Automatica.Core.Logic.Logic
    {
        internal const int Length = 64;

        private readonly RuleInterfaceInstance _output;
        private readonly bool[] _bits = new bool[Length];

        private int _bit = ShiftRegisterLogicFactory.DefaultBit;
        private bool _data;
        private bool _down;

        public ShiftRegisterRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == ShiftRegisterLogicFactory.RuleOutput);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == ShiftRegisterLogicFactory.RuleParamBit)
            {
                _bit = System.Math.Clamp(OneHotOutputs.ToIndex(value) ?? ShiftRegisterLogicFactory.DefaultBit, 1, Length);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            if (template == ShiftRegisterLogicFactory.RuleData)
            {
                _data = Helper.ConvertValueToBool(value);
            }
            else if (template == ShiftRegisterLogicFactory.RuleDirection)
            {
                _down = Helper.ConvertValueToBool(value);
            }
            else if (template == ShiftRegisterLogicFactory.RuleTrigger && Helper.ConvertValueToBool(value))
            {
                if (_down)
                {
                    Array.Copy(_bits, 1, _bits, 0, Length - 1);
                    _bits[Length - 1] = _data;
                }
                else
                {
                    Array.Copy(_bits, 0, _bits, 1, Length - 1);
                    _bits[0] = _data;
                }

                return SingleOutputChanged(new LogicOutputChanged(_output, _bits[_bit - 1]));
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
