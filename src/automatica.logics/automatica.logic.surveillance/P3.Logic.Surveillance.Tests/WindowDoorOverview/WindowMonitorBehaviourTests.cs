using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Logics;
using Newtonsoft.Json.Linq;
using P3.Logic.Surveillance.WindowMonitor;
using Xunit;

// LogicTest uses a process-wide dispatcher singleton, so the test classes must not run in parallel
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Logic.Surveillance.Tests.WindowDoorOverview
{
    public class WindowMonitorBehaviourTests : LogicTest<WindowMonitorFactory>
    {
        private readonly IDispatchable _window1 = new DispatchableMock("Window1");
        private readonly IDispatchable _window2 = new DispatchableMock("Window2");

        private WindowMonitorLogic Monitor => (WindowMonitorLogic)Logic;

        private void Input(Guid template, IDispatchable source, object value)
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(template), source, value));
        }

        private WindowState StateOf(IDispatchable source)
        {
            return Monitor.States[source.Id].Item2;
        }

        public static IEnumerable<object[]> TruthTable => new List<object[]>
        {
            // contact true means closed, the inverted inputs mean the opposite
            new object[] { WindowMonitorFactory.Ct, true, WindowState.Closed },
            new object[] { WindowMonitorFactory.Ct, false, WindowState.Tilted },
            new object[] { WindowMonitorFactory.Cti, true, WindowState.Tilted },
            new object[] { WindowMonitorFactory.Cti, false, WindowState.Closed },
            new object[] { WindowMonitorFactory.Co, true, WindowState.Closed },
            new object[] { WindowMonitorFactory.Co, false, WindowState.Open },
            new object[] { WindowMonitorFactory.Coi, true, WindowState.Open },
            new object[] { WindowMonitorFactory.Coi, false, WindowState.Closed },
        };

        [Theory]
        [MemberData(nameof(TruthTable))]
        public void Input_Bool_MapsToWindowState(Guid template, bool value, WindowState expected)
        {
            Input(template, _window1, value);

            Assert.Single(Monitor.States);
            Assert.Equal(expected, StateOf(_window1));
        }

        public static IEnumerable<object[]> CoercionData => new List<object[]>
        {
            new object[] { 1, WindowState.Closed },
            new object[] { 0, WindowState.Tilted },
            new object[] { 1.5d, WindowState.Closed },
            new object[] { 0d, WindowState.Tilted },
            new object[] { double.NaN, WindowState.Closed },
            new object[] { "true", WindowState.Closed },
            new object[] { "False", WindowState.Tilted },
            new object[] { null, WindowState.Tilted },
        };

        [Theory]
        [MemberData(nameof(CoercionData))]
        public void Input_ConvertibleValues_AreCoercedToBool(object value, WindowState expected)
        {
            Input(WindowMonitorFactory.Ct, _window1, value);

            Assert.Equal(expected, StateOf(_window1));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("open")]
        [InlineData("")]
        public void Input_NotConvertibleString_IsIgnored(string value)
        {
            Input(WindowMonitorFactory.Co, _window1, value);

            Assert.Empty(Monitor.States);
        }

        [Fact]
        public void Input_NotConvertibleString_KeepsThePreviousState()
        {
            Input(WindowMonitorFactory.Co, _window1, false);
            Input(WindowMonitorFactory.Co, _window1, "garbage");

            Assert.Equal(WindowState.Open, StateOf(_window1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LockedInputs_DoNotChangeTheState(bool value)
        {
            Input(WindowMonitorFactory.Cl, _window1, value);
            Input(WindowMonitorFactory.Cli, _window2, value);

            Assert.Empty(Monitor.States);
        }

        [Fact]
        public void LockedInput_AfterOpen_KeepsOpen()
        {
            Input(WindowMonitorFactory.Co, _window1, false);
            Input(WindowMonitorFactory.Cl, _window1, true);

            Assert.Equal(WindowState.Open, StateOf(_window1));
        }

        [Fact]
        public void SameSource_OpenThenClosed_UpdatesTheState()
        {
            Input(WindowMonitorFactory.Co, _window1, false);
            Assert.Equal(WindowState.Open, StateOf(_window1));

            Input(WindowMonitorFactory.Co, _window1, true);

            Assert.Single(Monitor.States);
            Assert.Equal(WindowState.Closed, StateOf(_window1));
        }

        [Fact]
        public void SameSource_DifferentInputs_LastValueWins()
        {
            Input(WindowMonitorFactory.Ct, _window1, false);
            Assert.Equal(WindowState.Tilted, StateOf(_window1));

            Input(WindowMonitorFactory.Co, _window1, false);
            Assert.Equal(WindowState.Open, StateOf(_window1));

            Input(WindowMonitorFactory.Coi, _window1, false);
            Assert.Equal(WindowState.Closed, StateOf(_window1));
        }

        [Fact]
        public void SameValueTwice_KeepsOneEntry()
        {
            Input(WindowMonitorFactory.Ct, _window1, false);
            Input(WindowMonitorFactory.Ct, _window1, false);

            Assert.Single(Monitor.States);
            Assert.Equal(WindowState.Tilted, StateOf(_window1));
        }

        [Fact]
        public void DifferentSources_AreTrackedSeparately()
        {
            Input(WindowMonitorFactory.Co, _window1, false);
            Input(WindowMonitorFactory.Ct, _window2, false);

            Assert.Equal(2, Monitor.States.Count);
            Assert.Equal(WindowState.Open, StateOf(_window1));
            Assert.Equal(WindowState.Tilted, StateOf(_window2));
            Assert.Same(_window1, Monitor.States[_window1.Id].Item1);
        }

        [Fact]
        public void Input_NullSource_IsIgnored()
        {
            Input(WindowMonitorFactory.Co, null, false);

            Assert.Empty(Monitor.States);
        }

        [Fact]
        public void Initial_NoStates_VisuDataIsEmpty()
        {
            Assert.Empty(Monitor.States);

            var data = Assert.IsType<JArray>(Logic.GetDataForVisu());
            Assert.Empty(data);
        }

        [Fact]
        public void VisuData_ContainsIdNameAndStatePerSource()
        {
            Input(WindowMonitorFactory.Co, _window1, false);
            Input(WindowMonitorFactory.Ct, _window2, true);

            var data = Assert.IsType<JArray>(Logic.GetDataForVisu());

            Assert.Equal(2, data.Count);
            var first = data.Single(a => a["Id"]!.ToObject<Guid>() == _window1.Id);
            Assert.Equal("Window1", first["Name"]!.ToObject<string>());
            Assert.Equal(WindowState.Open, first["Value"]!.ToObject<WindowState>());

            var second = data.Single(a => a["Id"]!.ToObject<Guid>() == _window2.Id);
            Assert.Equal("Window2", second["Name"]!.ToObject<string>());
            Assert.Equal(WindowState.Closed, second["Value"]!.ToObject<WindowState>());
        }

        [Fact]
        public void VisuData_ReflectsTheLatestState()
        {
            Input(WindowMonitorFactory.Co, _window1, false);
            Input(WindowMonitorFactory.Co, _window1, true);

            var data = Assert.IsType<JArray>(Logic.GetDataForVisu());

            Assert.Equal(WindowState.Closed, Assert.Single(data)["Value"]!.ToObject<WindowState>());
        }
    }
}
