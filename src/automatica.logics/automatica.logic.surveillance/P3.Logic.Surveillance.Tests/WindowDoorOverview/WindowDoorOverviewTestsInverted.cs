using Automatica.Core.Base.IO;
using P3.Logic.Surveillance.WindowMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.UnitTests.Base.Logics;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Test.Addition
{
    public class WindowMonitorTestsInverted: LogicTest<WindowMonitorFactory>
    {
        private IDispatchable WindowX1 = new DispatchableMock("WindowX1");
        private IDispatchable WindowX2 = new DispatchableMock("WindowX2");
        private IDispatchable WindowX3 = new DispatchableMock("WindowX3");
        private IDispatchable WindowX4 = new DispatchableMock("WindowX4");
        private IDispatchable WindowX5 = new DispatchableMock("WindowX5");

        [Fact]
        public void WindowMonitorTests_Tilt1()
        {
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX1, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX2, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX3, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX5, true);

            var data = windowMonitorLogic.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            foreach (var state in dic.Values)
            {
                Assert.Equal(WindowState.Tilted, state.Item2);
            }
        }

        [Fact]
        public void WindowMonitorTests__Tilt2()
        {
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX1, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX2, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX3, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cti), WindowX5, true);

            var data = windowMonitorLogic.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            var closed = dic.Values.Where(a => a.Item2 == WindowState.Closed).ToList();
            Assert.Equal(3, closed.Count);

            var tilt = dic.Values.Where(a => a.Item2 == WindowState.Tilted).ToList();
            Assert.Equal(2, tilt.Count);
        }
        [Fact]
        public void WindowMonitorTests_Closed1()
        {
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX1, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX2, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX3, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX5, true);

            var data = windowMonitorLogic.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            foreach (var state in dic.Values)
            {
                Assert.Equal(WindowState.Open, state.Item2);
            }
        }

        [Fact]
        public void WindowMonitorTests__Closed2()
        {
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX1, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX2, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX3, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Coi), WindowX5, true);

            var data = windowMonitorLogic.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            var closed = dic.Values.Where(a => a.Item2 == WindowState.Closed).ToList();
            Assert.Equal(3, closed.Count);

            var tilt = dic.Values.Where(a => a.Item2 == WindowState.Open).ToList();
            Assert.Equal(2, tilt.Count);
        }


        [Fact]
        public void WindowMonitorTests_Locked1()
        {
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX1, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX2, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX3, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX5, true);

            var data = windowMonitorLogic.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

           // Assert.Equal(5, dic.Count);

            //foreach (var state in dic.Values)
            //{
            //    Assert.Equal(WindowState.Unlocked, state.Item2);
            //}
        }

        [Fact]
        public void WindowMonitorTests__Locked2()
        {
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX1, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX2, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX3, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cli), WindowX5, true);

            var data = windowMonitorLogic.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

//            Assert.Equal(5, dic.Count);

            //var closed = dic.Values.Where(a => a.Item2 == WindowState.Locked).ToList();
            //Assert.Equal(3, closed.Count);

            //var tilt = dic.Values.Where(a => a.Item2 == WindowState.Unlocked).ToList();
            //Assert.Equal(2, tilt.Count);
        }
    }
}
