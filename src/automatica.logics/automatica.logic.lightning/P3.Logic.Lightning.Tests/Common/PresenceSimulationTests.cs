using System;
using System.Linq;
using Automatica.Core.EF.Models;
using P3.Logic.Lightning.Common;
using Xunit;

namespace P3.Logic.Lightning.Tests.Common
{
    public class PresenceSimulationTests
    {
        private static readonly DateTime Monday = new(2026, 11, 9);

        private static TimerPropertyData Window(int startHour, int stopHour, params DayOfWeek[] days) => new()
        {
            StartTime = Monday.AddHours(startHour),
            StopTime = Monday.AddHours(stopHour),
            EnabledDays = days.Length == 0 ? Enum.GetValues<DayOfWeek>().ToList() : days.ToList()
        };

        private static PresenceSimulation Create(int seed = 1)
        {
            var sim = new PresenceSimulation { Window = Window(18, 23), Enabled = true };
            sim.UseRandom(new Random(seed));
            return sim;
        }

        [Fact]
        public void Disabled_DoesNotTouchTheOutput()
        {
            var sim = Create();
            sim.Enabled = false;

            var step = sim.Evaluate(Monday.AddHours(19));

            Assert.Null(step.State);
            Assert.Null(step.Next);
            Assert.False(sim.IsSimulating);
        }

        [Fact]
        public void ModeOff_IgnoresPs()
        {
            var sim = Create();
            sim.Mode = PresenceSimulationMode.Off;

            Assert.Null(sim.Evaluate(Monday.AddHours(19)).State);
        }

        [Fact]
        public void BeforeTheWindow_WaitsForItsStart()
        {
            var sim = Create();

            var step = sim.Evaluate(Monday.AddHours(12));

            Assert.Null(step.State);
            Assert.Equal(Monday.AddHours(18), step.Next);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(42)]
        public void Plan_HasMinToMaxActivationsOfMinToMaxMinutes_InsideTheWindow(int seed)
        {
            var sim = Create(seed);
            sim.MinActivations = 2;
            sim.MaxActivations = 3;
            sim.MinDurationMinutes = 20;
            sim.MaxDurationMinutes = 60;

            sim.Evaluate(Monday.AddHours(18));
            var plan = sim.Plan;

            Assert.InRange(plan.Count, 2, 3);
            foreach (var (start, end) in plan)
            {
                Assert.True(start >= Monday.AddHours(18));
                Assert.True(end <= Monday.AddHours(23));
                Assert.InRange((end - start).TotalMinutes, 19.9, 60.1);
            }
            for (var i = 1; i < plan.Count; i++)
            {
                Assert.True(plan[i].Start >= plan[i - 1].End, "activations must not overlap");
            }
        }

        [Fact]
        public void Plan_TooLongForTheWindow_IsShrunk()
        {
            var sim = Create();
            sim.Window = Window(18, 19);
            sim.MinActivations = 4;
            sim.MaxActivations = 4;
            sim.MinDurationMinutes = 120;
            sim.MaxDurationMinutes = 180;

            sim.Evaluate(Monday.AddHours(18));

            Assert.Equal(4, sim.Plan.Count);
            Assert.True(sim.Plan.Sum(a => (a.End - a.Start).TotalMinutes) <= 60);
            Assert.True(sim.Plan.Last().End <= Monday.AddHours(19));
        }

        [Fact]
        public void Evaluate_FollowsThePlan_AndSwitchesOffAtTheWindowEnd()
        {
            var sim = Create(7);
            var step = sim.Evaluate(Monday.AddHours(18));
            Assert.False(step.State);
            var plan = sim.Plan.ToList();
            Assert.NotEmpty(plan);
            Assert.Equal(plan[0].Start, step.Next);

            foreach (var (start, end) in plan)
            {
                var on = sim.Evaluate(start);
                Assert.True(on.State);
                Assert.Equal(end, on.Next);

                var off = sim.Evaluate(end);
                Assert.False(off.State);
            }

            // the plan stays the same during the window
            Assert.Equal(plan, sim.Plan.ToList());

            var afterWindow = sim.Evaluate(Monday.AddHours(23));
            Assert.False(afterWindow.State);
            Assert.False(sim.IsSimulating);
            Assert.Equal(Monday.AddDays(1).AddHours(18), afterWindow.Next);

            // nothing more to switch until the next window
            Assert.Null(sim.Evaluate(Monday.AddHours(23).AddMinutes(1)).State);
        }

        [Fact]
        public void NextWindow_GetsANewPlan()
        {
            var sim = Create(3);
            sim.Evaluate(Monday.AddHours(18));
            var first = sim.Plan.ToList();
            sim.Evaluate(Monday.AddHours(23));

            sim.Evaluate(Monday.AddDays(1).AddHours(18));

            Assert.NotEmpty(sim.Plan);
            Assert.All(sim.Plan, a => Assert.True(a.Start >= Monday.AddDays(1).AddHours(18)));
            Assert.NotEqual(first, sim.Plan.ToList());
        }

        [Fact]
        public void PsGoesOff_SwitchesOffOnce()
        {
            var sim = Create();
            sim.Evaluate(Monday.AddHours(18));
            Assert.True(sim.IsSimulating);

            sim.Enabled = false;
            Assert.False(sim.Evaluate(Monday.AddHours(19)).State);
            Assert.Null(sim.Evaluate(Monday.AddHours(19).AddMinutes(1)).State);
        }

        [Fact]
        public void WindowAcrossMidnight_IsActiveAfterMidnight()
        {
            var sim = Create();
            sim.Window = Window(22, 2, DayOfWeek.Monday);

            var step = sim.Evaluate(Monday.AddDays(1).AddHours(1));

            Assert.True(sim.IsSimulating);
            Assert.NotNull(step.State);
            Assert.True(step.Next <= Monday.AddDays(1).AddHours(2));
        }

        [Fact]
        public void DisabledDay_IsSkipped()
        {
            var sim = Create();
            sim.Window = Window(18, 23, DayOfWeek.Wednesday);

            var step = sim.Evaluate(Monday.AddHours(19));

            Assert.Null(step.State);
            Assert.Equal(Monday.AddDays(2).AddHours(18), step.Next);
        }

        [Fact]
        public void NoWindow_NeverSimulates()
        {
            var sim = Create();
            sim.Window = null;

            var step = sim.Evaluate(Monday.AddHours(19));

            Assert.Null(step.State);
            Assert.Null(step.Next);
        }

        [Fact]
        public void Replay_RepeatsTheChangesOfOneWeekAgo()
        {
            var sim = Create();
            sim.Mode = PresenceSimulationMode.Replay;
            sim.Enabled = false;
            var lastWeek = Monday.AddDays(-7);
            sim.Record(lastWeek.AddHours(19), true);
            sim.Record(lastWeek.AddHours(20).AddMinutes(15), false);
            sim.Record(lastWeek.AddHours(21), true);
            sim.Record(lastWeek.AddHours(22), false);
            sim.Enabled = true;

            var step = sim.Evaluate(Monday.AddHours(18));
            Assert.False(step.State);
            Assert.Equal(Monday.AddHours(19), step.Next);

            step = sim.Evaluate(Monday.AddHours(19));
            Assert.True(step.State);
            Assert.Equal(Monday.AddHours(20).AddMinutes(15), step.Next);

            step = sim.Evaluate(Monday.AddHours(20).AddMinutes(15));
            Assert.False(step.State);

            step = sim.Evaluate(Monday.AddHours(21).AddMinutes(30));
            Assert.True(step.State);
            Assert.Equal(Monday.AddHours(22), step.Next);
        }

        [Fact]
        public void Replay_WithoutRecording_FallsBackToRandom()
        {
            var sim = Create();
            sim.Mode = PresenceSimulationMode.Replay;

            sim.Evaluate(Monday.AddHours(18));

            Assert.NotEmpty(sim.Plan);
        }

        [Fact]
        public void Record_WhileSimulating_IsIgnored()
        {
            var sim = Create();
            sim.Evaluate(Monday.AddHours(18));

            Assert.False(sim.Record(Monday.AddHours(19), true));
            Assert.Empty(sim.Events);
        }

        [Fact]
        public void Record_SkipsRepeatsAndOldEvents()
        {
            var sim = Create();
            sim.Enabled = false;

            Assert.False(sim.Record(Monday, false));
            Assert.True(sim.Record(Monday.AddHours(1), true));
            Assert.False(sim.Record(Monday.AddHours(2), true));
            Assert.True(sim.Record(Monday.AddHours(3), false));

            // more than 8 days later the old ones are dropped
            Assert.True(sim.Record(Monday.AddDays(9), true));
            Assert.Single(sim.Events);
        }

        [Fact]
        public void Events_RoundTripThroughJson()
        {
            var sim = Create();
            sim.Enabled = false;
            sim.Record(Monday.AddHours(19), true);
            sim.Record(Monday.AddHours(20), false);

            var copy = new PresenceSimulation();
            copy.LoadEvents(sim.SerializeEvents());

            Assert.Equal(sim.Events, copy.Events);
        }

        [Fact]
        public void LoadEvents_BrokenJson_IsIgnored()
        {
            var sim = new PresenceSimulation();
            sim.LoadEvents("{not json");
            Assert.Empty(sim.Events);
        }

        [Fact]
        public void ParseWindow_AcceptsObjectAndJson()
        {
            var window = Window(18, 23);
            Assert.Same(window, PresenceSimulation.ParseWindow(window));

            var parsed = PresenceSimulation.ParseWindow(Newtonsoft.Json.JsonConvert.SerializeObject(window));
            Assert.Equal(window.StartTime, parsed.StartTime);
            Assert.Equal(7, parsed.EnabledDays.Count);

            Assert.Null(PresenceSimulation.ParseWindow("nope"));
            Assert.Null(PresenceSimulation.ParseWindow(5));
        }

        [Fact]
        public void Binding_Step_OnlyReportsChanges()
        {
            var binding = new PresenceSimulationBinding(PresenceSimulationIds.Derive("test"));
            binding.Simulation.UseRandom(new Random(5));
            binding.Simulation.Window = Window(18, 23);
            binding.ApplyInput(binding.Ids.Enable, true);

            var first = binding.Step(Monday.AddHours(18));
            Assert.False(first.State);

            var again = binding.Step(Monday.AddHours(18).AddSeconds(1));
            Assert.Null(again.State);
            Assert.Equal(first.Next, again.Next);

            var on = binding.Step(binding.Simulation.Plan[0].Start);
            Assert.True(on.State);
        }

        [Fact]
        public void Binding_ApplyParameter_ParsesAndClamps()
        {
            var ids = PresenceSimulationIds.Derive("test");
            var binding = new PresenceSimulationBinding(ids);

            Assert.True(binding.ApplyParameter(ids.Mode, 2L));
            Assert.Equal(PresenceSimulationMode.Replay, binding.Simulation.Mode);
            Assert.True(binding.ApplyParameter(ids.Mode, "9"));
            Assert.Equal(PresenceSimulationMode.Random, binding.Simulation.Mode);

            binding.ApplyParameter(ids.MinActivations, -3);
            Assert.Equal(0, binding.Simulation.MinActivations);
            binding.ApplyParameter(ids.MaxDuration, "0");
            Assert.Equal(1, binding.Simulation.MaxDurationMinutes);

            Assert.False(binding.ApplyParameter(Guid.NewGuid(), 1));
            Assert.False(binding.ApplyInput(ids.Mode, true));
        }
    }
}
