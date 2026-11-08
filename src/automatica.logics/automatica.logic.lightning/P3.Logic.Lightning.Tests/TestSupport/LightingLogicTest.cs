using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.Tests.TestSupport
{
    /// <summary>
    /// Runs a <see cref="ClockedLogic"/> against the fake clock and a recording dispatcher; the timer never runs, the test moves
    /// the clock (<see cref="At"/>, <see cref="Advance"/>) and fires the tick.
    /// </summary>
    public abstract class LightingLogicTest<TFactory> : LogicTest<TFactory>, IDisposable where TFactory : LogicFactory
    {
        internal RecordingDispatcher Recorder { get; private set; } = new();

        internal ClockedLogic Sut { get; private set; }

        /// <summary>a weekday in November, far from a daylight saving change</summary>
        protected static readonly DateTime Monday = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Local);

        protected LightingLogicTest()
        {
            // the base class starts an instance on the shared dispatcher - not used here
            Logic.Stop().GetAwaiter().GetResult();
            At(Monday.AddHours(12));
            Sut = Create();
        }

        private ClockedLogic Create()
        {
            var sut = (ClockedLogic)Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
            sut.TimerEnabled = false;
            return sut;
        }

        protected DateTime Now => FakeTimeProvider.GetLocalNow().DateTime;

        protected void At(DateTime local)
        {
            FakeTimeProvider.SetDateTime(DateTime.SpecifyKind(local, DateTimeKind.Local));
        }

        /// <summary>
        /// Moves the clock and fires the tick when the logic's deadline is reached
        /// </summary>
        protected void Advance(TimeSpan by)
        {
            var target = Now + by;
            // fire every deadline on the way, in order
            while (Sut.NextTick.HasValue && Sut.NextTick.Value <= target)
            {
                At(Sut.NextTick.Value);
                Sut.Tick();
            }
            At(target);
        }

        protected void AdvanceSeconds(double seconds) => Advance(TimeSpan.FromSeconds(seconds));

        protected void Start() => Sut.Start().GetAwaiter().GetResult();

        protected void Stop() => Sut.Stop().GetAwaiter().GetResult();

        /// <summary>
        /// A new logic instance on the same rule instance and dispatcher, as after a restart of the core
        /// </summary>
        protected void Restart()
        {
            Stop();
            Sut = Create();
            Start();
        }

        protected RuleInterfaceInstance Interface(Guid template) =>
            GetLogicInterfaceByTemplate(template) ?? throw new ArgumentException($"no interface {template}");

        protected void Param(Guid template, object value)
        {
            Interface(template).Value = value;
        }

        /// <summary>
        /// Changes a parameter at runtime
        /// </summary>
        protected void SetParam(Guid template, object value)
        {
            Param(template, value);
            Sut.ValueChanged(Interface(template), Dispatchable, value);
        }

        protected void Input(Guid template, object value)
        {
            Sut.ValueChanged(Interface(template), Dispatchable, value);
        }

        /// <summary>
        /// A pulse: 1 then 0
        /// </summary>
        protected void Pulse(Guid template)
        {
            Input(template, true);
            Input(template, false);
        }

        protected IReadOnlyList<object> Values(Guid output) => Recorder.ValuesOf(Interface(output).ObjId);

        protected object Last(Guid output) => Values(output).LastOrDefault();

        protected void ClearRecorded() => Recorder.ClearDispatched();

        public virtual void Dispose()
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
}
