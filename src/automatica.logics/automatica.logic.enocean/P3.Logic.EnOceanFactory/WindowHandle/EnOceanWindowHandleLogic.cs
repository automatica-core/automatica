using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Logic.EnOceanFactory.WindowHandle
{
    public class EnOceanWindowHandleLogic : Rule
    {
        private readonly RuleInterfaceInstance _output;

        public EnOceanWindowHandleLogic(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == EnOceanWindowHandleLogicFactory.RuleOutput);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == EnOceanWindowHandleLogicFactory.RuleInput)
            {
                var numericValue = Convert.ToInt32(value);

                switch (numericValue)
                {
                    case 13:
                        return SingleOutputChanged(new RuleOutputChanged(_output, 0));
                    case 12:
                    case 14:
                        return SingleOutputChanged(new RuleOutputChanged(_output, 1));
                    case 15:
                        return SingleOutputChanged(new RuleOutputChanged(_output, 2));

                }
            }
            return base.InputValueChanged(instance, source, value);
        }
    }
}
