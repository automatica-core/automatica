using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.BinaryDecoder
{
    public class BinaryDecoderRule : Automatica.Core.Logic.Logic
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

        public BinaryDecoderRule(ILogicContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleOutput1);
            _output2 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleOutput2);
            _output3 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleOutput3);
            _output4 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleOutput4);
            _output5 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleOutput5);
            _output6 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleOutput6);
            _output7 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleOutput7);
            _output8 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleOutput8);


        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _b1 = null;
            return Task.FromResult(true);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null && instance.This2RuleInterfaceTemplate == BinaryDecoderLogicFactory.RuleInput1)
            {
                var i1 = Convert.ToInt64(value);
                _b1 = BitConverter.GetBytes(i1)[0];

            }

            if (_b1.HasValue)
            {
                return new List<ILogicOutputChanged>
                {
                    new LogicOutputChanged(_output1, (_b1.Value & 1) > 0),
                    new LogicOutputChanged(_output2, (_b1.Value & 2) > 0),
                    new LogicOutputChanged(_output3, (_b1.Value & 4) > 0),
                    new LogicOutputChanged(_output4, (_b1.Value & 8) > 0),
                    new LogicOutputChanged(_output5, (_b1.Value & 16) > 0),
                    new LogicOutputChanged(_output6, (_b1.Value & 32) > 0),
                    new LogicOutputChanged(_output7, (_b1.Value & 64) > 0),
                    new LogicOutputChanged(_output8, (_b1.Value & 128) > 0),
                };
            }
            return new List<ILogicOutputChanged>();
        }

    }
}
