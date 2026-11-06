using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Random
{
    public class RandomRule : Automatica.Core.Logic.Logic
    {
        private double? _i1;

        private RuleInterfaceInstance _output;
        private RuleInterfaceInstance _minInstance;
        private RuleInterfaceInstance _maxInstance;


        private bool _disabled = false;

        private readonly System.Random _random = new System.Random();

        public RandomRule(ILogicContext context) : base(context)
        {
            ResolveInterfaces(context.RuleInstance);
        }

        private void ResolveInterfaces(RuleInstance ruleInstance)
        {
            _output = ruleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomLogicFactory.RuleOutput);

            _minInstance = ruleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomLogicFactory.RuleParamMin);

            _maxInstance = ruleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomLogicFactory.RuleParamMax);
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            // on a reload the instance is a fresh copy, the cached parameter instances would be stale
            ResolveInterfaces(instance);
            return base.Start(instance, token);
        }

        public int Min
        {
            get
            {
                if (_minInstance != null && _minInstance.ValueInteger.HasValue)
                {
                    return (int)_minInstance.ValueInteger.Value;
                }

                return 0;
            }
        }
        public int Max
        {
            get
            {
                if (_maxInstance != null && _maxInstance.ValueInteger.HasValue)
                {
                    return (int)_maxInstance.ValueInteger.Value;
                }

                return 100;
            }
        }

        private static bool IsTruthy(object value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool b:
                    return b;
                case string s:
                    if (bool.TryParse(s, out var sb))
                    {
                        return sb;
                    }
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sd) && sd != 0;
                case IConvertible c:
                    try
                    {
                        return c.ToDouble(CultureInfo.InvariantCulture) != 0;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        /// <summary>
        /// A random integer in [Min, Max] - both bounds are inclusive
        /// </summary>
        internal int NextValue()
        {
            long min = Min;
            long max = Max;
            if (max < min)
            {
                (min, max) = (max, min);
            }

            // NextInt64 excludes the upper bound, long arithmetic avoids the overflow for int.MaxValue
            return (int)_random.NextInt64(min, max + 1);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == RandomLogicFactory.RuleInputDisabled)
            {
                // only changes the state, nothing to emit
                _disabled = IsTruthy(value);
                return new List<ILogicOutputChanged>();
            }

            if (instance.This2RuleInterfaceTemplate == RandomLogicFactory.RuleInputTrigger && !_disabled && IsTruthy(value))
            {
                _i1 = NextValue();
                return SingleOutputChanged(new LogicOutputChanged(_output, _i1));
            }

            return new List<ILogicOutputChanged>();
        }

    }
}
