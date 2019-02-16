using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Logic.BaseOperations.BinaryDecoder
{
    public class BinaryDecoderRule : Automatica.Core.Rule.Rule
    {
        private byte? _b1 = null;


        private readonly RuleInterfaceInstance _output1;
        private readonly RuleInterfaceInstance _output2;
        private readonly RuleInterfaceInstance _output3;
        private readonly RuleInterfaceInstance _output4;
        private readonly RuleInterfaceInstance _output5;
        private readonly RuleInterfaceInstance _output6;
        private readonly RuleInterfaceInstance _output7;
        private readonly RuleInterfaceInstance _output8;

        public BinaryDecoderRule(IRuleContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleOutput1);
            _output2 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleOutput2);
            _output3 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleOutput3);
            _output4 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleOutput4);
            _output5 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleOutput5);
            _output6 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleOutput6);
            _output7 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleOutput7);
            _output8 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleOutput8);


        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null && instance.This2RuleInterfaceTemplate == BinaryDecoderRuleFactory.RuleInput1)
            {
                var i1 = Convert.ToInt64(value);
                _b1 = BitConverter.GetBytes(i1)[0];

            }

            if (_b1.HasValue)
            {
                return new List<IRuleOutputChanged>
                {
                    new RuleOutputChanged(_output1, (_b1.Value & 1) > 0),
                    new RuleOutputChanged(_output2, (_b1.Value & 2) > 0),
                    new RuleOutputChanged(_output3, (_b1.Value & 4) > 0),
                    new RuleOutputChanged(_output4, (_b1.Value & 8) > 0),
                    new RuleOutputChanged(_output5, (_b1.Value & 16) > 0),
                    new RuleOutputChanged(_output6, (_b1.Value & 32) > 0),
                    new RuleOutputChanged(_output7, (_b1.Value & 64) > 0),
                    new RuleOutputChanged(_output8, (_b1.Value & 128) > 0),
                };
            }
            return new List<IRuleOutputChanged>();
        }

    }
}
