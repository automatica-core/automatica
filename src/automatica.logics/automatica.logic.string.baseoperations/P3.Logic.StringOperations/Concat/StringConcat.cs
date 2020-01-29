using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Logic.StringOperations.Concat
{
    public class StringConcat : Rule
    {
        private readonly RuleInterfaceInstance _output;

        private string _i1 = String.Empty;
        private string _i2 = String.Empty;
        private string _i3 = String.Empty;
        private string _i4 = String.Empty;

        public StringConcat(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == StringConcatFactory.RuleOutput);
        }           

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
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

            return SingleOutputChanged(new RuleOutputChanged(_output, $"{_i1}{_i2}{_i3}{_i4}"));
        }
    }
}
