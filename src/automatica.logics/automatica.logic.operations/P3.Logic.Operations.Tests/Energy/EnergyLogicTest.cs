using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Energy;

namespace P3.Logic.Operations.Tests.Energy
{
    /// <summary>
    /// Runs an energy logic with a manual clock and a recording dispatcher. The timer ticks are run by the test (<see cref="Tick"/>).
    /// </summary>
    public abstract class EnergyLogicTest<TFactory> : LogicTest<TFactory>, IDisposable where TFactory : LogicFactory
    {
        private bool _started;

        protected EnergyLogicTest()
        {
            // the base class starts an instance that dispatches into the shared dispatcher - it is not used here
            Logic.Stop().GetAwaiter().GetResult();
            Sut = (EnergyLogicBase)Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
            Sut.Clock = () => Now;
        }

        internal RecordingDispatcher Recorder { get; } = new();

        internal EnergyLogicBase Sut { get; }

        protected DateTimeOffset Now { get; set; } = new(2026, 6, 21, 10, 0, 0, TimeSpan.Zero);

        protected RuleInterfaceInstance Interface(Guid template) => GetLogicInterfaceByTemplate(template);

        protected void Param(Guid template, object value)
        {
            Interface(template).Value = value;
            if (_started)
            {
                Sut.ValueChanged(Interface(template), Dispatchable, value);
            }
        }

        protected void Start()
        {
            Sut.Start().GetAwaiter().GetResult();
            _started = true;
        }

        protected IList<ILogicOutputChanged> Input(Guid template, object value) => Sut.ValueChanged(Interface(template), Dispatchable, value);

        /// <summary>
        /// Moves the clock and runs one timer tick
        /// </summary>
        protected void Advance(double seconds)
        {
            Now = Now.AddSeconds(seconds);
            Sut.Tick();
        }

        /// <summary>
        /// Runs a tick every second for the given time
        /// </summary>
        protected void Run(double seconds)
        {
            for (var i = 0; i < seconds; i++)
            {
                Advance(1);
            }
        }

        protected IReadOnlyList<object> Values(Guid output) => Recorder.ValuesOf(output);

        protected object Last(Guid output) => Recorder.LastOf(output);

        public virtual void Dispose()
        {
            if (_started)
            {
                Sut.Stop().GetAwaiter().GetResult();
            }
            GC.SuppressFinalize(this);
        }
    }
}
