using System;
using System.Collections.Generic;
using System.Linq;
using P3.Logic.StringOperations.StatusMonitor;
using P3.Logic.StringOperations.Tests.TestSupport;
using Xunit;

namespace P3.Logic.StringOperations.Tests.StatusMonitor
{
    public class StatusMonitorTests : TextLogicTest<StatusMonitorLogicFactory>
    {
        private readonly NamedSource _kitchen = new("Kitchen window");
        private readonly NamedSource _bath = new("Bath window");
        private readonly NamedSource _door = new("Front door");

        private StatusMonitorRule Monitor => (StatusMonitorRule)Sut;

        private static Guid Count(int state) => StatusMonitorLogicFactory.OutputCount[state - 1];

        private void Windows()
        {
            // 1 = open, 0 = closed
            Param(StatusMonitorLogicFactory.ParamValue[0], "1");
            Param(StatusMonitorLogicFactory.ParamText[0], "open");
            Param(StatusMonitorLogicFactory.ParamColor[0], "#e53935");
            Param(StatusMonitorLogicFactory.ParamValue[1], "0");
            Param(StatusMonitorLogicFactory.ParamText[1], "closed");
            Param(StatusMonitorLogicFactory.ParamRestText, "unknown");
            Start();
        }

        private void Stat(NamedSource source, object value) => Input(StatusMonitorLogicFactory.InputStatus, value, source);

        [Fact]
        public void CountsObjectsPerState()
        {
            Windows();

            Stat(_kitchen, true);
            Stat(_bath, false);
            Stat(_door, 0);

            Assert.Equal(1, Last(Count(1)));
            Assert.Equal(2, Last(Count(2)));
            Assert.Equal(0, Last(StatusMonitorLogicFactory.OutputRest));
            // unused states are counted too (0)
            Assert.Equal(0, Last(Count(3)));

            Stat(_bath, true);
            Assert.Equal(2, Last(Count(1)));
            Assert.Equal(1, Last(Count(2)));
        }

        [Fact]
        public void ValuesWithoutState_CountAsRest()
        {
            Windows();
            Stat(_kitchen, "jammed");
            Stat(_bath, 0.5);

            Assert.Equal(2, Last(StatusMonitorLogicFactory.OutputRest));
            Assert.Equal(0, Last(Count(1)));
        }

        [Fact]
        public void LastChange_NameAndStateText_NotOnTheFirstValue()
        {
            Windows();
            Stat(_kitchen, false);
            Assert.Empty(Values(StatusMonitorLogicFactory.OutputLastChange));

            Stat(_kitchen, true);
            Assert.Equal("Kitchen window: open", Last(StatusMonitorLogicFactory.OutputLastChange));

            // the same state again is no change
            ClearRecorded();
            Stat(_kitchen, 1);
            Assert.Empty(Values(StatusMonitorLogicFactory.OutputLastChange));
            Assert.Empty(Values(Count(1)));

            // a change to "no state" is not reported on Txlc
            Stat(_kitchen, "?");
            Assert.Empty(Values(StatusMonitorLogicFactory.OutputLastChange));
            Assert.Equal(1, Last(StatusMonitorLogicFactory.OutputRest));
        }

        [Fact]
        public void ActivityLog_NewestFirst_Limited()
        {
            Param(StatusMonitorLogicFactory.ParamLogEntries, 2);
            Windows();
            Stat(_kitchen, false);
            Stat(_kitchen, true);
            Stat(_kitchen, false);
            Stat(_kitchen, true);

            var log = Monitor.Log;
            Assert.Equal(2, log.Count);
            Assert.Equal("open", log[0].Text);
            Assert.Equal("closed", log[1].Text);
            Assert.Equal("Kitchen window", log[0].Name);
        }

        [Fact]
        public void ActivityLog_Zero_IsOff()
        {
            Param(StatusMonitorLogicFactory.ParamLogEntries, 0);
            Windows();
            Stat(_kitchen, false);
            Stat(_kitchen, true);
            Assert.Empty(Monitor.Log);
            Assert.Equal("Kitchen window: open", Last(StatusMonitorLogicFactory.OutputLastChange));
        }

        [Fact]
        public void PriorityIsTheStateOrder_AndAlternatives()
        {
            Param(StatusMonitorLogicFactory.ParamValue[0], "error|fault");
            Param(StatusMonitorLogicFactory.ParamValue[1], "FAULT");
            Start();

            Stat(_kitchen, "Fault");
            Assert.Equal(1, Last(Count(1)));
            Assert.Equal(0, Last(Count(2)));
            Assert.Equal(1, Monitor.Objects.Single().State);
        }

        [Fact]
        public void TextStates_IgnoreCase_BoolStatesByName()
        {
            Param(StatusMonitorLogicFactory.ParamValue[0], "charging");
            Param(StatusMonitorLogicFactory.ParamValue[1], "true");
            Start();

            Stat(_kitchen, "Charging");
            Stat(_bath, true);
            Assert.Equal(1, Last(Count(1)));
            Assert.Equal(1, Last(Count(2)));
        }

        [Fact]
        public void ChangedStateValues_MapTheObjectsAgain()
        {
            Windows();
            Stat(_kitchen, 2);
            Assert.Equal(1, Last(StatusMonitorLogicFactory.OutputRest));

            ChangeParam(StatusMonitorLogicFactory.ParamValue[2], "2");
            Assert.Equal(1, Last(Count(3)));
            Assert.Equal(0, Last(StatusMonitorLogicFactory.OutputRest));
        }

        [Fact]
        public void StateWithoutText_UsesTheValue()
        {
            Param(StatusMonitorLogicFactory.ParamValue[0], "eco");
            Start();
            Stat(_kitchen, "x");
            Stat(_kitchen, "eco");
            Assert.Equal("Kitchen window: eco", Last(StatusMonitorLogicFactory.OutputLastChange));
        }

        [Fact]
        public void VisuData_StatesObjectsAndLog()
        {
            Windows();
            Stat(_kitchen, false);
            Stat(_bath, true);
            Stat(_door, "?");
            Stat(_kitchen, true);

            var monitor = Data<Dictionary<string, object>>("monitor");
            var states = ((List<object>)monitor["states"]).Cast<Dictionary<string, object>>().ToList();
            // only the configured states
            Assert.Equal(2, states.Count);
            Assert.Equal("open", states[0]["text"]);
            Assert.Equal("#e53935", states[0]["color"]);
            Assert.Equal(2, states[0]["count"]);
            Assert.Equal(1, ((Dictionary<string, object>)monitor["rest"])["count"]);
            Assert.Equal("unknown", ((Dictionary<string, object>)monitor["rest"])["text"]);

            var objects = (List<Dictionary<string, object>>)monitor["objects"];
            Assert.Equal(3, objects.Count);
            // state order, then the name; the rest last
            Assert.Equal(new[] { "Bath window", "Kitchen window", "Front door" }, objects.Select(a => (string)a["name"]).ToArray());

            var log = (List<Dictionary<string, object>>)monitor["log"];
            Assert.Single(log);
            Assert.Equal("Kitchen window: open", monitor["lastChange"]);
        }
    }
}
