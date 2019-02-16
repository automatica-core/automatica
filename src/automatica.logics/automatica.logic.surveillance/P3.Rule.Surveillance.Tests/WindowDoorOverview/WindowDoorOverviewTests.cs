using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Surveillance.WindowMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace P3.Rule.Math.BasicOperations.Test.Addition
{
    public class WindowMonitorTests : RuleTest<WindowMonitorFactory>
    {
        private IDispatchable WindowX1 = new DispatchableMock("WindowX1");
        private IDispatchable WindowX2 = new DispatchableMock("WindowX2");
        private IDispatchable WindowX3 = new DispatchableMock("WindowX3");
        private IDispatchable WindowX4 = new DispatchableMock("WindowX4");
        private IDispatchable WindowX5 = new DispatchableMock("WindowX5");

        [Fact]
        public void WindowMonitorTests_Tilt1()
        {
            var windowMonitorRule = (WindowMonitorRule)Rule;

            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX1, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX2, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX3, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX4, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX5, true);

            var data = windowMonitorRule.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            foreach (var state in dic.Values)
            {
                Assert.Equal(WindowState.Closed, state.Item2);
            }
        }

        [Fact]
        public void WindowMonitorTests__Tilt2()
        {
            var windowMonitorRule = (WindowMonitorRule)Rule;

            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX1, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX2, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX3, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX4, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX5, true);

            var data = windowMonitorRule.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            var closed = dic.Values.Where(a => a.Item2 == WindowState.Closed).ToList();
            Assert.Equal(2, closed.Count);

            var tilt = dic.Values.Where(a => a.Item2 == WindowState.Tilt).ToList();
            Assert.Equal(3, tilt.Count);
        }
        [Fact]
        public void WindowMonitorTests_Closed1()
        {
            var windowMonitorRule = (WindowMonitorRule)Rule;

            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX1, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX2, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX3, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX4, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX5, true);

            var data = windowMonitorRule.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            foreach (var state in dic.Values)
            {
                Assert.Equal(WindowState.Closed, state.Item2);
            }
        }

        [Fact]
        public void WindowMonitorTests__Closed2()
        {
            var windowMonitorRule = (WindowMonitorRule)Rule;

            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX1, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX2, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX3, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX4, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Co), WindowX5, true);

            var data = windowMonitorRule.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            var closed = dic.Values.Where(a => a.Item2 == WindowState.Closed).ToList();
            Assert.Equal(2, closed.Count);

            var tilt = dic.Values.Where(a => a.Item2 == WindowState.Open).ToList();
            Assert.Equal(3, tilt.Count);
        }


        [Fact]
        public void WindowMonitorTests_Locked1()
        {
            var windowMonitorRule = (WindowMonitorRule)Rule;

            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX1, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX2, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX3, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX4, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX5, true);

            var data = windowMonitorRule.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            foreach (var state in dic.Values)
            {
                Assert.Equal(WindowState.Locked, state.Item2);
            }
        }

        [Fact]
        public void WindowMonitorTests__Locked2()
        {
            var windowMonitorRule = (WindowMonitorRule)Rule;

            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX1, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX2, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX3, false);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX4, true);
            Rule.ValueChanged(GetRuleInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX5, true);

            var data = windowMonitorRule.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            var closed = dic.Values.Where(a => a.Item2 == WindowState.Locked).ToList();
            Assert.Equal(2, closed.Count);

            var tilt = dic.Values.Where(a => a.Item2 == WindowState.Unlocked).ToList();
            Assert.Equal(3, tilt.Count);
        }
    }
}
