using System;
using System.Collections.Generic;
using System.Reflection;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;

namespace P3.Logic.Time.Tests.TestSupport
{
    /// <summary>
    /// Runs a time logic against a fixed clock (<see cref="FakeTimeProvider"/>), a pinned time zone and a recording dispatcher.
    /// Timer ticks are fired by the test (<see cref="FireTick"/>) instead of waiting for the real <see cref="System.Timers.Timer"/>.
    /// </summary>
    public abstract class TimeLogicTest<TFactory> : LogicTest<TFactory>, IDisposable where TFactory : LogicFactory
    {
        private readonly TimeZoneScope _zone;
        private bool _started;

        internal RecordingDispatcher Recorder { get; } = new();

        /// <summary>
        /// The logic under test, it dispatches into <see cref="Recorder"/>
        /// </summary>
        protected ILogic Sut { get; }

        protected TimeLogicTest() : this(TimeZoneInfo.Utc)
        {
        }

        protected TimeLogicTest(TimeZoneInfo zone)
        {
            // the base class starts an instance that dispatches into the shared dispatcher - it is not used here
            Logic.Stop().GetAwaiter().GetResult();

            _zone = TimeZoneScope.Of(zone);
            Sut = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
        }

        protected void SetUtcNow(DateTime utc)
        {
            if (utc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("use a UTC instant", nameof(utc));
            }
            FakeTimeProvider.SetDateTime(utc);
        }

        protected void Start()
        {
            Sut.Start().GetAwaiter().GetResult();
            _started = true;
        }

        protected void Stop()
        {
            Sut.Stop().GetAwaiter().GetResult();
            _started = false;
        }

        protected RuleInterfaceInstance Interface(Guid template) => GetLogicInterfaceByTemplate(template);

        protected IList<ILogicOutputChanged> Input(Guid template, object value)
        {
            return Sut.ValueChanged(Interface(template), Dispatchable, value);
        }

        protected IReadOnlyList<object> Values(Guid output) => Recorder.ValuesOf(output);

        protected object Last(Guid output) => Recorder.LastOf(output);

        protected void ClearRecorded() => Recorder.ClearValues().GetAwaiter().GetResult();

        protected System.Timers.Timer SutTimer => (System.Timers.Timer)GetField("_timer");

        protected object GetField(string name)
        {
            var field = FindField(Sut.GetType(), name) ?? throw new MissingFieldException(Sut.GetType().Name, name);
            return field.GetValue(Sut);
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    return field;
                }
            }
            return null;
        }

        /// <summary>
        /// Runs the logic's timer tick handler synchronously, as if its <see cref="System.Timers.Timer"/> had elapsed
        /// </summary>
        protected void FireTick()
        {
            var method = Sut.GetType().GetMethod("_timer_Elapsed", BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? throw new MissingMethodException(Sut.GetType().Name, "_timer_Elapsed");
            method.Invoke(Sut, new object[] { null, null });
        }

        public virtual void Dispose()
        {
            if (_started)
            {
                try
                {
                    Sut.Stop().GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    // a test that leaves the logic in a broken state must not hide its own failure
                }
            }
            // stop whatever timer is left so it cannot tick into the next test
            try
            {
                if (FindField(Sut.GetType(), "_timer")?.GetValue(Sut) is System.Timers.Timer t)
                {
                    t.Stop();
                }
            }
            catch (Exception)
            {
            }
            _zone.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
