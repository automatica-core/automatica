using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Climate.Common;
using Xunit;

namespace P3.Logic.Operations.Tests.Climate
{
    /// <summary>
    /// Climate block under test with its own clock. The timer of the block is stopped, the test calls <see cref="Tick"/>.
    /// </summary>
    public abstract class ClimateTest<T> : LogicTest<T> where T : LogicFactory
    {
        protected DateTime Time = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

        protected ClimateTest()
        {
            Logic.Stop().GetAwaiter().GetResult();

            // RuleInterfaceInstance.Value parses the default "0.5" with the current culture (de: 5) - send the invariant default text again
            foreach (var param in RuleInstance.RuleInterfaceInstance.Where(a =>
                         a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)Automatica.Core.Base.Templates.LogicInterfaceDirection.Param))
            {
                Logic.ValueChanged(param, Dispatchable, param.This2RuleInterfaceTemplateNavigation.DefaultValue);
            }

            Climate.Clock = () => Time;
            Climate.Tick();
        }

        internal ClimateLogic Climate => (ClimateLogic)Logic;

        protected IList<ILogicOutputChanged> Send(Guid input, object value) =>
            Logic.ValueChanged(GetLogicInterfaceByTemplate(input), Dispatchable, value);

        protected void SetParam(Guid param, object value)
        {
            var instance = GetLogicInterfaceByTemplate(param);
            Assert.NotNull(instance);
            Logic.ValueChanged(instance, Dispatchable, value);
        }

        /// <summary>
        /// Advances the clock in steps of <paramref name="step"/> seconds, ticking after every step
        /// </summary>
        protected void Run(double seconds, double step = 1)
        {
            var left = seconds;
            while (left > 1e-9)
            {
                var s = Math.Min(step, left);
                Time = Time.AddSeconds(s);
                Climate.Tick();
                left -= s;
            }
        }

        protected IList<ILogicOutputChanged> Tick() => Climate.Tick();

        protected object Out(Guid output) => Climate.LastOutput(output);

        protected double Num(Guid output)
        {
            var value = Out(output);
            Assert.NotNull(value);
            return Convert.ToDouble(value);
        }

        protected bool Flag(Guid output)
        {
            var value = Out(output);
            Assert.NotNull(value);
            return Convert.ToBoolean(value);
        }

        protected static object Value(IList<ILogicOutputChanged> changes, Guid output) =>
            changes.Single(a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == output).Value;
    }
}
