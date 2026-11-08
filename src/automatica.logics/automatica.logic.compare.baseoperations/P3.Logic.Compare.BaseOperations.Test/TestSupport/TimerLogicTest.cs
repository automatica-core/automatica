using System;
using System.Collections.Generic;
using System.Reflection;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;

namespace P3.Logic.Compare.BaseOperations.Tests.TestSupport
{
    /// <summary>
    /// Runs a logic that dispatches from timers against a recording dispatcher.
    /// Timer ticks are fired by the test (<see cref="FireTick"/>) instead of waiting for the real <see cref="System.Timers.Timer"/>.
    /// </summary>
    public abstract class TimerLogicTest<TFactory> : LogicTest<TFactory>, IDisposable where TFactory : LogicFactory
    {
        private bool _started;

        internal RecordingDispatcher Recorder { get; } = new();

        /// <summary>
        /// The logic under test, it dispatches into <see cref="Recorder"/>
        /// </summary>
        protected ILogic Sut { get; }

        protected TimerLogicTest()
        {
            // the base class starts an instance that dispatches into the shared dispatcher - it is not used here
            Logic.Stop().GetAwaiter().GetResult();
            Sut = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
        }

        protected void Start()
        {
            Sut.Start().GetAwaiter().GetResult();
            _started = true;
        }

        protected RuleInterfaceInstance Interface(Guid template) => GetLogicInterfaceByTemplate(template);

        protected IList<ILogicOutputChanged> Input(Guid template, object value)
        {
            return Sut.ValueChanged(Interface(template), Dispatchable, value);
        }

        protected IReadOnlyList<object> Values(Guid output) => Recorder.ValuesOf(output);

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
        protected void FireTick(string handler = "_timer_Elapsed")
        {
            var method = Sut.GetType().GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? throw new MissingMethodException(Sut.GetType().Name, handler);
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
            GC.SuppressFinalize(this);
        }
    }
}
