﻿using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.Bigger
{
    public class BiggerRule : Automatica.Core.Logic.Logic
    {
        private double? _i1 = null;
        private double? _i2 = null;


        private readonly RuleInterfaceInstance _output;

        public BiggerRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == BiggerLogicFactory.RuleOutput);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == BiggerLogicFactory.RuleInput1)
                {
                    _i1 = Convert.ToDouble(value);
                }

                if (instance.This2RuleInterfaceTemplate == BiggerLogicFactory.RuleInput2)
                {
                    _i2 = Convert.ToDouble(value);
                }
            }

            if (_i1 == null || _i2 == null)
            {
                return new List<ILogicOutputChanged>();
            }


            return SingleOutputChanged(new LogicOutputChanged(_output, _i1 > _i2));
        }

    }
}