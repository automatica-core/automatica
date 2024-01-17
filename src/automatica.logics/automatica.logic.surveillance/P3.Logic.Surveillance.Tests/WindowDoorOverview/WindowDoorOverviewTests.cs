using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Surveillance.WindowMonitor;
using Xunit;

namespace P3.Logic.Surveillance.Tests.WindowDoorOverview
{
    public class WindowMonitorTests: LogicTest<WindowMonitorFactory>
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

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX1, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX2, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX3, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX5, true);

            var data = windowMonitorLogic.States;

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
            var windowMonitorRule = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX1, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX2, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX3, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Ct), WindowX5, true);

            var data = windowMonitorRule.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

            Assert.Equal(5, dic.Count);

            var closed = dic.Values.Where(a => a.Item2 == WindowState.Closed).ToList();
            Assert.Equal(2, closed.Count);

            var tilt = dic.Values.Where(a => a.Item2 == WindowState.Tilted).ToList();
            Assert.Equal(3, tilt.Count);
        }
        [Fact]
        public void WindowMonitorTests_Closed1()
        {
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX1, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX2, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX3, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX5, true);

            var data = windowMonitorLogic.States;

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
            var windowMonitorRule = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX1, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX2, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX3, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Co), WindowX5, true);

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
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX1, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX2, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX3, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX5, true);

            var data = windowMonitorLogic.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

         //   Assert.Equal(5, dic.Count);

            //foreach (var state in dic.Values)
            //{
            //    Assert.Equal(WindowState.Locked, state.Item2);
            //}
        }

        [Fact]
        public void WindowMonitorTests__Locked2()
        {
            var windowMonitorLogic = (WindowMonitorLogic)Logic;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX1, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX2, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX3, false);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX4, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(WindowMonitorFactory.Cl), WindowX5, true);

            var data = windowMonitorLogic.States;

            Assert.IsAssignableFrom<IDictionary<Guid, Tuple<IDispatchable, WindowState>>>(data);

            var dic = data;

           // Assert.Equal(5, dic.Count);

            //var closed = dic.Values.Where(a => a.Item2 == WindowState.Locked).ToList();
            //Assert.Equal(2, closed.Count);

            //var tilt = dic.Values.Where(a => a.Item2 == WindowState.Unlocked).ToList();
            //Assert.Equal(3, tilt.Count);
        }
    }
}
