using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Newtonsoft.Json;
using P3.Logic.Operations.Irrigation;
using Xunit;
using F = P3.Logic.Operations.Irrigation.IrrigationLogicFactory;

namespace P3.Logic.Operations.Tests.GarageMedia
{
    public class IrrigationTests : BlockTest<F>
    {
        private void Durations(params int[] seconds)
        {
            for (var i = 0; i < F.ParamDurations.Length; i++)
            {
                Param(F.ParamDurations[i], i < seconds.Length ? seconds[i] : 0);
            }
        }

        private object[] ValveStates => F.OutputValves.Select(Last).ToArray();
        private int Active => (int)Last(F.OutputActiveValve);
        private IReadOnlyCollection<IrrigationHistoryEntry> History => (IrrigationHistoryEntry[])VisuData["history"];

        [Fact]
        public void Factory_Defaults()
        {
            Assert.Equal(600L, Interface(F.ParamDurations[0]).ValueInteger);
            Assert.Equal(0L, Interface(F.ParamDurations[7]).ValueInteger);
            Assert.Equal(2.0, Interface(F.ParamMaxRainForecast).Value);
            Assert.Equal(1800L, Interface(F.ParamMaxRainTime).ValueInteger);
            Assert.IsType<TimerPropertyData>(Interface(F.ParamSchedule).Value);
        }

        [Fact]
        public void Start_RunsTheZonesOneAfterTheOther()
        {
            Durations(60, 0, 120);
            Start();
            Input(F.InputStart, true);

            Assert.Equal(1, Active);
            Assert.Equal(true, Last(F.OutputPump));
            Assert.Equal(true, Last(F.OutputValves[0]));
            Assert.Equal(60000, Timer().Interval);
            Assert.Equal(true, Last(F.OutputRunning));

            Clock.Advance(60);
            Fire();
            // zone 2 has no time
            Assert.Equal(3, Active);
            Assert.Equal(false, Last(F.OutputValves[0]));
            Assert.Equal(true, Last(F.OutputValves[2]));
            Assert.Equal(120000, Timer().Interval);

            Clock.Advance(120);
            Fire();
            Assert.Equal(0, Active);
            Assert.Equal(false, Last(F.OutputPump));
            Assert.Equal(false, Last(F.OutputRunning));
            Assert.All(F.OutputValves, a => Assert.NotEqual(true, Last(a)));
            Assert.Equal("done", History.First().Kind);
        }

        [Fact]
        public void NextValve_OpensBeforeThePreviousCloses_PumpNeverAgainstClosedValves()
        {
            Durations(10, 10);
            Start();
            Input(F.InputStart, true);
            Fire();
            Input(F.InputStop, true);

            var open = new HashSet<Guid>();
            var pump = false;
            foreach (var (id, value) in Recorder.All())
            {
                if (id == F.OutputPump)
                {
                    pump = (bool)value;
                }
                else if (F.OutputValves.Contains(id))
                {
                    if ((bool)value)
                    {
                        open.Add(id);
                    }
                    else
                    {
                        open.Remove(id);
                    }
                }
                Assert.False(pump && open.Count == 0, "pump on with all valves closed");
            }
        }

        [Fact]
        public void Activate_SkipsWhenRainIsForecast()
        {
            Durations(60);
            Start();
            Input(F.InputRainForecast, 2.5);
            Input(F.InputActivate, true);

            Assert.Equal(0, Active);
            Assert.Equal(true, Last(F.OutputSkipped));
            Assert.Equal("skip", History.First().Kind);

            // start in the visualization runs anyway
            Input(F.InputStart, true);
            Assert.Equal(1, Active);
            Assert.Equal(false, Last(F.OutputSkipped));
        }

        [Fact]
        public void Activate_SkipsAfterLongRainInTheLast24Hours()
        {
            Durations(60);
            Param(F.ParamMaxRainTime, 1800);
            Start();
            Input(F.InputRain, true);
            Clock.Advance(1900);
            Input(F.InputRain, false);
            Assert.Equal(1900.0, Last(F.OutputRainTime));

            Input(F.InputActivate, true);
            Assert.Equal(true, Last(F.OutputSkipped));

            // a day later the rain is forgotten
            Clock.Advance(24 * 3600);
            Input(F.InputActivate, true);
            Assert.Equal(1, Active);
        }

        [Fact]
        public void RainTime_CountsOnlyTheLast24Hours()
        {
            Start();
            Input(F.InputRain, true);
            Clock.Advance(3600);
            Input(F.InputRain, false);
            Clock.Advance(23 * 3600 + 1800);
            Fire("_clockTimer_Elapsed");
            Assert.Equal(1800.0, Last(F.OutputRainTime));
        }

        [Fact]
        public void AdjustByRain_ShortensTheZones()
        {
            Durations(600, 300);
            Param(F.ParamAdjustByRain, true);
            Param(F.ParamMaxRainForecast, 4.0);
            Start();
            Input(F.InputRainForecast, 1.0);
            Input(F.InputActivate, true);

            Assert.Equal(1, Active);
            Assert.Equal(450000, Timer().Interval);
            Assert.Equal(0.75, VisuData["factor"]);
        }

        [Fact]
        public void Select_OpensOneValve_AndStopsAfterItsTime()
        {
            Durations(60, 90);
            Start();
            Input(F.InputSelect, 2);
            Assert.Equal(2, Active);
            Assert.Equal(true, Last(F.OutputValves[1]));
            Assert.Equal(90000, Timer().Interval);

            Fire();
            Assert.Equal(0, Active);
            Assert.Equal(false, Last(F.OutputPump));
        }

        [Fact]
        public void Select_Nine_OpensAllValves_ZeroStops()
        {
            Durations(60, 90);
            Start();
            Input(F.InputSelect, 9);
            Assert.Equal(9, Active);
            Assert.All(F.OutputValves, a => Assert.Equal(true, Last(a)));
            Assert.Equal(90000, Timer().Interval);

            Input(F.InputSelect, 0);
            Assert.Equal(0, Active);
            Assert.All(F.OutputValves, a => Assert.Equal(false, Last(a)));
        }

        [Fact]
        public void Lock_StopsAndBlocks()
        {
            Durations(60);
            Start();
            Input(F.InputStart, true);
            Input(F.InputLock, true);
            Assert.Equal(0, Active);
            Input(F.InputStart, true);
            Assert.Equal(0, Active);
            Input(F.InputLock, false);
            Input(F.InputStart, true);
            Assert.Equal(1, Active);
        }

        [Fact]
        public void Schedule_StartsOncePerEnabledDay()
        {
            Durations(60);
            var schedule = new TimerPropertyData
            {
                StartTime = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc),
                StopTime = new DateTime(2026, 1, 1, 7, 0, 0, DateTimeKind.Utc),
                EnabledDays = new List<DayOfWeek> { DayOfWeek.Monday }
            };
            Param(F.ParamSchedule, JsonConvert.SerializeObject(schedule));
            Param(F.ParamScheduleEnabled, true);
            Clock.Set(new DateTimeOffset(2026, 6, 1, 5, 59, 0, TimeSpan.Zero)); // a Monday
            Start();

            Fire("_clockTimer_Elapsed");
            Assert.NotEqual(1, Last(F.OutputActiveValve));

            Clock.Set(new DateTimeOffset(2026, 6, 1, 6, 0, 30, TimeSpan.Zero));
            Fire("_clockTimer_Elapsed");
            Assert.Equal(1, Active);

            Input(F.InputStop, true);
            Fire("_clockTimer_Elapsed");
            Assert.Equal(0, Active);

            // Tuesday: not enabled
            Clock.Set(new DateTimeOffset(2026, 6, 2, 6, 0, 30, TimeSpan.Zero));
            Fire("_clockTimer_Elapsed");
            Assert.Equal(0, Active);

            // next Monday, but more than an hour late: no catch-up
            Clock.Set(new DateTimeOffset(2026, 6, 8, 7, 30, 0, TimeSpan.Zero));
            Fire("_clockTimer_Elapsed");
            Assert.Equal(0, Active);
        }

        [Fact]
        public void Schedule_Disabled_DoesNothing()
        {
            Durations(60);
            Param(F.ParamSchedule, JsonConvert.SerializeObject(new TimerPropertyData
            {
                StartTime = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc),
                EnabledDays = Enum.GetValues<DayOfWeek>().ToList()
            }));
            Clock.Set(new DateTimeOffset(2026, 6, 1, 6, 0, 30, TimeSpan.Zero));
            Start();
            Fire("_clockTimer_Elapsed");
            Assert.Equal(0, Active);
        }

        [Fact]
        public void Remaining_CountsDown()
        {
            Durations(300);
            Start();
            Input(F.InputStart, true);
            Assert.Equal(300.0, Last(F.OutputRemaining));
            Clock.Advance(120);
            Fire("_clockTimer_Elapsed");
            Assert.Equal(180.0, Last(F.OutputRemaining));
        }

        [Fact]
        public void VisuData_HasZonesAndHistory()
        {
            Param(F.ParamZoneNames, "Lawn; Hedge");
            Durations(60, 60);
            Start();
            Input(F.InputStart, true);
            var data = VisuData;
            var names = (string[])data["zoneNames"];
            Assert.Equal("Lawn", names[0]);
            Assert.Equal("Hedge", names[1]);
            Assert.Equal("3", names[2]);
            Assert.Equal(new[] { 60, 60, 0, 0, 0, 0, 0, 0 }, (int[])data["durations"]);
            Assert.Equal(new[] { "zone", "manual" }, History.Select(a => a.Kind).ToArray());
        }
    }
}
