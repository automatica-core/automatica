using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.Base
{
    public abstract class BaseCompareRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _inpu1;
        private readonly RuleInterfaceInstance _input2;

        private readonly RuleInterfaceInstance _output;


        private double? _i1;
        private double? _i2;

        private bool? _outValue;

        protected BaseCompareRule(ILogicContext context, Guid i1, Guid i2, Guid o) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == o);
            _inpu1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == i1);
            _input2 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == i2);
        }

        protected abstract bool Compare(double i1, double i2);

        protected sealed override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.ObjId == _inpu1.ObjId)
                {
                    if (value is DateTime vdt)
                    {
                        _i1 = new DateTimeOffset(vdt).ToUnixTimeSeconds();
                    }
                    else if (value is DateOnly vd)
                    {
                        _i1 = new DateTimeOffset(vd.Year, vd.Month, vd.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                    }
                    else if (value is TimeOnly vt)
                    {
                        _i1 = vt.Ticks;
                    }
                    else
                    {
                        _i1 = Convert.ToDouble(value);
                    }
                }

                if (instance.ObjId == _input2.ObjId)
                {
                    if (value is DateTime vdt)
                    {
                        _i2 = new DateTimeOffset(vdt).ToUnixTimeSeconds();
                    }
                    else if (value is DateOnly vd)
                    {
                        _i2 = new DateTimeOffset(vd.Year, vd.Month, vd.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                    }
                    else if (value is TimeOnly vt)
                    {
                        _i2 = vt.Ticks;
                    }
                    else
                    {
                        _i2 = Convert.ToDouble(value);
                    }
                }
            }

            if (_i1 == null || _i2 == null)
            {
                return new List<ILogicOutputChanged>();
            }

            var compareValue = Compare(_i1.Value, _i2.Value);

            if (_outValue != compareValue)
            {
                _outValue = compareValue;
                return SingleOutputChanged(new LogicOutputChanged(_output, _outValue));
            }

            return new List<ILogicOutputChanged>();

        }
    }
}
