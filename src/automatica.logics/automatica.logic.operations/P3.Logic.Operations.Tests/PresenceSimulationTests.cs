using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Common;
using P3.Logic.Operations.Dimmer;
using P3.Logic.Operations.Switch;
using P3.Logic.Operations.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Operations.Tests
{
    /// <summary>
    /// Runs a Switch/Dimmer logic with a recording dispatcher against the fake clock; the presence simulation timer never runs,
    /// the test moves the clock and fires the ticks.
    /// </summary>
    public abstract class PresenceSimulationTestBase<TFactory> : LogicTest<TFactory>, IDisposable where TFactory : LogicFactory
    {
        /// <summary>a weekday in November, far from a daylight saving change</summary>
        protected static readonly DateTime Monday = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Local);

        internal RecordingDispatcher Recorder { get; } = new();
        protected ILogic Sut { get; private set; }

        protected PresenceSimulationTestBase()
        {
            // the base class starts an instance on the shared dispatcher - not used here
            Logic.Stop().GetAwaiter().GetResult();
            At(Monday.AddHours(12));
            Window(18, 23);
            Sut = Create();
        }

        protected abstract PresenceSimulationIds Ids { get; }
        internal abstract PresenceSimulationHost Host(ILogic logic);

        internal PresenceSimulationHost Ps => Host(Sut);

        protected ILogic Create()
        {
            var logic = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
            Host(logic).TimerEnabled = false;
            Host(logic).Binding.Simulation.UseRandom(new Random(42));
            return logic;
        }

        protected DateTime Now => FakeTimeProvider.GetLocalNow().DateTime;

        protected void At(DateTime local) => FakeTimeProvider.SetDateTime(DateTime.SpecifyKind(local, DateTimeKind.Local));

        /// <summary>
        /// Moves the clock and fires every deadline on the way
        /// </summary>
        protected void AdvanceTo(DateTime target)
        {
            while (Ps.NextTick.HasValue && Ps.NextTick.Value <= target)
            {
                At(Ps.NextTick.Value);
                Ps.Tick();
            }
            At(target);
        }

        protected void Start() => Sut.Start().GetAwaiter().GetResult();

        protected void Restart()
        {
            Sut.Stop().GetAwaiter().GetResult();
            Sut = Create();
            Start();
        }

        protected RuleInterfaceInstance Interface(Guid template) => GetLogicInterfaceByTemplate(template);

        protected void Param(Guid template, object value) => Interface(template).Value = value;

        protected void Window(int startHour, int stopHour)
        {
            Param(Ids.Window, new TimerPropertyData
            {
                StartTime = new DateTime(2026, 1, 1, startHour, 0, 0),
                StopTime = new DateTime(2026, 1, 1, stopHour, 0, 0),
                EnabledDays = Enum.GetValues<DayOfWeek>().ToList()
            });
        }

        protected IList<ILogicOutputChanged> Input(Guid template, object value) =>
            Sut.ValueChanged(Interface(template), Dispatchable, value);

        protected void SetPs(bool enabled) => Input(Ids.Enable, enabled);

        protected IReadOnlyList<object> Values(Guid output) => Recorder.ValuesOf(Interface(output).ObjId);

        public void Dispose()
        {
            try
            {
                Sut.Stop().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }
            GC.SuppressFinalize(this);
        }
    }

    public class SwitchPresenceSimulationTests : PresenceSimulationTestBase<SwitchLogicFactory>
    {
        protected override PresenceSimulationIds Ids => SwitchLogicFactory.PresenceSimulation;
        internal override PresenceSimulationHost Host(ILogic logic) => ((SwitchLogic)logic).PresenceSimulation;

        private IReadOnlyList<object> Output => Values(SwitchLogicFactory.RuleOutput);

        [Fact]
        public void Factory_HasThePsInputAndParameters()
        {
            foreach (var id in new[] { Ids.Enable, Ids.Mode, Ids.Window, Ids.MinActivations, Ids.MaxActivations, Ids.MinDuration, Ids.MaxDuration })
            {
                Assert.NotNull(Interface(id));
            }
            Assert.Equal((int)PresenceSimulationMode.Random, Convert.ToInt32(Interface(Ids.Mode).Value));
            Assert.Equal(new Version(1, 1, 0, 0), Instance.LogicVersion);
        }

        [Fact]
        public void Random_SwitchesThePlannedActivationsInsideTheWindow()
        {
            Param(Ids.MinActivations, 2);
            Param(Ids.MaxActivations, 3);
            Param(Ids.MinDuration, 20.0);
            Param(Ids.MaxDuration, 40.0);
            Start();
            SetPs(true);

            // before the window nothing happens, the next evaluation is the window start
            Assert.Empty(Output);
            Assert.Equal(Monday.AddHours(18), Ps.NextTick);

            AdvanceTo(Monday.AddHours(18));
            var plan = Ps.Binding.Simulation.Plan;
            Assert.InRange(plan.Count, 2, 3);
            foreach (var (start, end) in plan)
            {
                Assert.True(start >= Monday.AddHours(18) && end <= Monday.AddHours(23));
                Assert.InRange((end - start).TotalMinutes, 19.9, 40.1);
            }

            // sample the output after every change: on exactly inside the planned activations
            var states = new List<(DateTime At, bool On)>();
            var count = 0;
            while (Ps.NextTick.HasValue && Ps.NextTick.Value <= Monday.AddHours(23))
            {
                AdvanceTo(Ps.NextTick.Value);
                if (Output.Count != count)
                {
                    count = Output.Count;
                    states.Add((Now, (bool)Output[^1]));
                }
            }

            Assert.Equal(plan.Count, states.Count(a => a.On));
            foreach (var (at, on) in states.Where(a => a.On))
            {
                Assert.Contains(plan, p => p.Start == at);
            }
            Assert.False((bool)Output[^1]);
            // first the simulation takes over (off), then on/off per activation; the window end sends nothing new
            Assert.Equal(1 + 2 * plan.Count, Output.Count);
            Assert.Equal(Monday.AddDays(1).AddHours(18), Ps.NextTick);
        }

        [Fact]
        public void Random_IsTheSameForTheSameSeed()
        {
            Start();
            SetPs(true);
            AdvanceTo(Monday.AddHours(18));
            var first = Ps.Binding.Simulation.Plan.ToList();

            var other = new PresenceSimulation();
            other.UseRandom(new Random(42));
            Assert.Equal(first, other.CreatePlan(Monday.AddHours(18), Monday.AddHours(23)));
        }

        [Fact]
        public void PsOff_SwitchesOffOnceAndStops()
        {
            Param(Ids.MinActivations, 1);
            Param(Ids.MaxActivations, 1);
            Start();
            SetPs(true);
            AdvanceTo(Monday.AddHours(18));
            var activation = Ps.Binding.Simulation.Plan.Single();
            AdvanceTo(activation.Start.AddMinutes(1));
            Assert.True((bool)Output[^1]);
            var before = Output.Count;

            SetPs(false);

            Assert.Equal(before + 1, Output.Count);
            Assert.False((bool)Output[^1]);
            Assert.Null(Ps.NextTick);
            Ps.Tick();
            Assert.Equal(before + 1, Output.Count);
        }

        [Fact]
        public void LongDurations_AreShrunkToFitTheWindow()
        {
            Param(Ids.MinActivations, 1);
            Param(Ids.MaxActivations, 1);
            Param(Ids.MinDuration, 600.0);
            Param(Ids.MaxDuration, 600.0);
            Window(18, 19);
            Start();
            SetPs(true);
            AdvanceTo(Monday.AddHours(18));
            var activation = Ps.Binding.Simulation.Plan.Single();
            Assert.True(activation.End <= Monday.AddHours(19));

            AdvanceTo(Monday.AddHours(19).AddMinutes(1));

            Assert.Equal(new object[] { false, true, false }, Output);
            Assert.False(Ps.Binding.IsSimulating);
        }

        [Fact]
        public void ModeOff_IgnoresPs()
        {
            Param(Ids.Mode, (int)PresenceSimulationMode.Off);
            Start();
            SetPs(true);
            AdvanceTo(Monday.AddHours(23));

            Assert.Empty(Output);
            Assert.Null(Ps.NextTick);
        }

        [Fact]
        public void Replay_SwitchesAsOneWeekAgo()
        {
            Param(Ids.Mode, (int)PresenceSimulationMode.Replay);
            Start();
            var lastMonday = Monday.AddDays(-7);
            At(lastMonday.AddHours(19));
            Input(SwitchLogicFactory.RuleInput, true);
            At(lastMonday.AddHours(20).AddMinutes(30));
            Input(SwitchLogicFactory.RuleInput, false);
            Recorder.ClearDispatched();

            At(Monday.AddHours(12));
            SetPs(true);
            AdvanceTo(Monday.AddHours(18).AddMinutes(1));
            Assert.Equal(new object[] { false }, Output);
            Assert.Equal(Monday.AddHours(19), Ps.NextTick);

            AdvanceTo(Monday.AddHours(19).AddMinutes(1));
            Assert.Equal(new object[] { false, true }, Output);
            Assert.Equal(Monday.AddHours(20).AddMinutes(30), Ps.NextTick);

            AdvanceTo(Monday.AddHours(21));
            Assert.Equal(new object[] { false, true, false }, Output);
        }

        [Fact]
        public void Replay_WindowEndWhileOn_SwitchesOffOnce()
        {
            Param(Ids.Mode, (int)PresenceSimulationMode.Replay);
            Start();
            At(Monday.AddDays(-7).AddHours(22));
            Input(SwitchLogicFactory.RuleInput, true);
            Recorder.ClearDispatched();

            At(Monday.AddHours(12));
            SetPs(true);
            AdvanceTo(Monday.AddHours(22).AddMinutes(30));
            Assert.Equal(new object[] { false, true }, Output);

            AdvanceTo(Monday.AddHours(23).AddMinutes(30));
            Assert.Equal(new object[] { false, true, false }, Output);
            Assert.False(Ps.Binding.IsSimulating);
            Assert.Equal(Monday.AddDays(1).AddHours(18), Ps.NextTick);
        }

        [Fact]
        public void Replay_WithoutRecording_FallsBackToRandom()
        {
            Param(Ids.Mode, (int)PresenceSimulationMode.Replay);
            Start();
            SetPs(true);
            AdvanceTo(Monday.AddHours(18));

            Assert.NotEmpty(Ps.Binding.Simulation.Plan);
        }

        [Fact]
        public void Recording_SurvivesARestart()
        {
            Start();
            At(Monday.AddHours(8));
            Input(SwitchLogicFactory.RuleInput, true);
            At(Monday.AddHours(9));
            Input(SwitchLogicFactory.RuleInput, false);

            Restart();

            var events = Ps.Binding.Simulation.Events;
            Assert.Equal(2, events.Count);
            Assert.Equal(new PresenceEvent(Monday.AddHours(8), true), events[0]);
            Assert.Equal(new PresenceEvent(Monday.AddHours(9), false), events[1]);
        }

        [Fact]
        public void SwitchApi_IsRecorded()
        {
            Start();
            var sw = (SwitchLogic)Sut;
            sw.SwitchOnAsync().GetAwaiter().GetResult();
            At(Now.AddMinutes(5));
            sw.SwitchOffAsync().GetAwaiter().GetResult();

            Assert.Equal(new[] { true, false }, Ps.Binding.Simulation.Events.Select(a => a.On));
        }

        [Fact]
        public void ChangesWhileSimulating_AreNotRecorded()
        {
            Param(Ids.MinActivations, 1);
            Param(Ids.MaxActivations, 1);
            Start();
            SetPs(true);
            AdvanceTo(Monday.AddHours(18));
            AdvanceTo(Ps.Binding.Simulation.Plan.Single().Start.AddMinutes(1));

            Assert.Empty(Ps.Binding.Simulation.Events);
        }

        [Fact]
        public void OldInstanceWithoutPsInterfaces_StillWorks()
        {
            foreach (var id in new[] { Ids.Enable, Ids.Mode, Ids.Window, Ids.MinActivations, Ids.MaxActivations, Ids.MinDuration, Ids.MaxDuration })
            {
                RuleInstance.RuleInterfaceInstance.Remove(Interface(id));
            }
            var logic = (SwitchLogic)Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
            logic.PresenceSimulation.TimerEnabled = false;

            Assert.True(logic.Start().GetAwaiter().GetResult());
            var outputs = logic.ValueChanged(GetLogicInterfaceByTemplate(SwitchLogicFactory.RuleInput), Dispatchable, true);

            Assert.Equal(true, outputs.Single().Value);
            Assert.Null(logic.PresenceSimulation.NextTick);
            logic.Stop().GetAwaiter().GetResult();
        }
    }

    public class DimmerPresenceSimulationTests : PresenceSimulationTestBase<DimmerLogicFactory>
    {
        protected override PresenceSimulationIds Ids => DimmerLogicFactory.PresenceSimulation;
        internal override PresenceSimulationHost Host(ILogic logic) => ((DimmerLogic)logic).PresenceSimulation;

        [Fact]
        public void Simulation_SwitchesOnWithTheLastBrightness_AndOffWithZero()
        {
            Param(Ids.MinActivations, 1);
            Param(Ids.MaxActivations, 1);
            Start();
            Input(DimmerLogicFactory.RuleInputValue, 60);
            Input(DimmerLogicFactory.RuleInputState, false);
            Recorder.ClearDispatched();

            SetPs(true);
            AdvanceTo(Monday.AddHours(18));
            var activation = Ps.Binding.Simulation.Plan.Single();
            AdvanceTo(activation.Start.AddMinutes(1));

            Assert.Equal(60, Values(DimmerLogicFactory.DimmerValue)[^1]);
            Assert.Equal(true, Values(DimmerLogicFactory.DimmerState)[^1]);
            Assert.Equal(Automatica.Core.Control.Base.SwitchState.On, ((DimmerLogic)Sut).State);

            AdvanceTo(activation.End.AddMinutes(1));

            Assert.Equal(0, Values(DimmerLogicFactory.DimmerValue)[^1]);
            Assert.Equal(false, Values(DimmerLogicFactory.DimmerState)[^1]);
        }

        [Fact]
        public void Simulation_WithoutBrightness_SwitchesOnWith100()
        {
            Param(Ids.MinActivations, 1);
            Param(Ids.MaxActivations, 1);
            Start();
            SetPs(true);
            AdvanceTo(Monday.AddHours(18));
            AdvanceTo(Ps.Binding.Simulation.Plan.Single().Start.AddMinutes(1));

            Assert.Equal(100, Values(DimmerLogicFactory.DimmerValue)[^1]);
        }

        [Fact]
        public void ManualChanges_AreRecorded()
        {
            Start();
            Input(DimmerLogicFactory.RuleInputValue, 40);
            At(Now.AddMinutes(1));
            Input(DimmerLogicFactory.RuleInputValue, 0);
            At(Now.AddMinutes(1));
            ((DimmerLogic)Sut).DimAsync(30).GetAwaiter().GetResult();

            Assert.Equal(new[] { true, false, true }, Ps.Binding.Simulation.Events.Select(a => a.On));
        }

        [Fact]
        public void Recording_SurvivesARestart()
        {
            Start();
            Input(DimmerLogicFactory.RuleInputState, true);

            Restart();

            Assert.Single(Ps.Binding.Simulation.Events);
        }
    }
}
