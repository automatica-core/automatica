using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.StringOperations.Concat
{
    public class StringConcat: Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;

        private string _i1 = String.Empty;
        private string _i2 = String.Empty;
        private string _i3 = String.Empty;
        private string _i4 = String.Empty;

        public StringConcat(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == StringConcatFactory.RuleOutput);
        }           

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            
            if (instance.This2RuleInterfaceTemplate == StringConcatFactory.RuleInput1)
            {
                _i1 = value?.ToString();
            }

            if (instance.This2RuleInterfaceTemplate == StringConcatFactory.RuleInput2)
            {
                _i2 = value?.ToString();
            }
            if (instance.This2RuleInterfaceTemplate == StringConcatFactory.RuleInput3)
            {
                _i3 = value?.ToString();
            }
            if (instance.This2RuleInterfaceTemplate == StringConcatFactory.RuleInput4)
            {
                _i4 = value?.ToString();
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, $"{_i1}{_i2}{_i3}{_i4}"));
        }
    }
}
