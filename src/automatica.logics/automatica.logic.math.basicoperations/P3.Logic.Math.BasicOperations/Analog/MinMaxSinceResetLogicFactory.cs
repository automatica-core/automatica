using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Analog
{
    public class MinMaxSinceResetLogicFactory : LogicFactory
    {
        public static readonly Guid RuleValue = new Guid("ac6c3c35-031c-4690-b27b-101692ca6e10");
        public static readonly Guid RuleReset = new Guid("1a6deebe-6915-441c-8844-b58442919844");
        public static readonly Guid RuleMinimum = new Guid("44d645de-6701-4fcc-b6b8-0aaad3ac28b9");
        public static readonly Guid RuleMaximum = new Guid("eade7a93-d4eb-45a4-9aad-2bdac72828e8");

        public override string LogicName => "Math.MinMaxSinceReset";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("2c698ec5-7611-4566-84bc-bc5ad57a5f9e");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.MIN_MAX_SINCE_RESET.NAME", "MATH.MIN_MAX_SINCE_RESET.DESCRIPTION", "math.min_max_since_reset", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleValue, "V", "MATH.MIN_MAX_SINCE_RESET.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "MATH.MIN_MAX_SINCE_RESET.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleMinimum, "Min", "MATH.MIN_MAX_SINCE_RESET.MINIMUM.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleMaximum, "Max", "MATH.MIN_MAX_SINCE_RESET.MAXIMUM.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MinMaxSinceResetRule(context);
        }
    }

    /// <summary>
    /// MinMax since reset (Loxone "MinMax since Reset"): Min and Max are the lowest and highest V since the last 1 on R.
    /// R sets both to the current V. Only changed outputs are sent.
    /// </summary>
    public class MinMaxSinceResetRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _minimum;
        private readonly RuleInterfaceInstance _maximum;

        private double? _value;
        private double? _min;
        private double? _max;

        public MinMaxSinceResetRule(ILogicContext context) : base(context)
        {
            _minimum = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == MinMaxSinceResetLogicFactory.RuleMinimum);
            _maximum = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == MinMaxSinceResetLogicFactory.RuleMaximum);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            var changes = new List<ILogicOutputChanged>();

            if (template == MinMaxSinceResetLogicFactory.RuleValue)
            {
                var v = MathValue.ToDouble(value);
                if (!v.HasValue)
                {
                    return changes;
                }

                _value = v;
                if (!_min.HasValue || v.Value < _min.Value)
                {
                    _min = v;
                    changes.Add(new LogicOutputChanged(_minimum, v.Value));
                }
                if (!_max.HasValue || v.Value > _max.Value)
                {
                    _max = v;
                    changes.Add(new LogicOutputChanged(_maximum, v.Value));
                }
            }
            else if (template == MinMaxSinceResetLogicFactory.RuleReset && MathValue.ToBool(value))
            {
                _min = _value;
                _max = _value;
                if (_value.HasValue)
                {
                    changes.Add(new LogicOutputChanged(_minimum, _value.Value));
                    changes.Add(new LogicOutputChanged(_maximum, _value.Value));
                }
            }

            return changes;
        }
    }
}
