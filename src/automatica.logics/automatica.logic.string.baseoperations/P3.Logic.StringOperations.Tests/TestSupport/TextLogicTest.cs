using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;

namespace P3.Logic.StringOperations.Tests.TestSupport
{
    /// <summary>
    /// Runs a text logic against a recording dispatcher. Values that the logic returns from an input change are recorded too,
    /// so <see cref="Values"/> shows everything the outputs got, in order. Timer ticks are fired by the test.
    /// </summary>
    public abstract class TextLogicTest<TFactory> : LogicTest<TFactory>, IDisposable where TFactory : LogicFactory
    {
        private bool _started;

        internal RecordingDispatcher Recorder { get; } = new();

        protected ILogic Sut { get; }

        protected TextLogicTest()
        {
            // the base class starts an instance on the shared dispatcher - not used here
            Logic.Stop().GetAwaiter().GetResult();
            Sut = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
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

        /// <summary>
        /// Sets a parameter before the start
        /// </summary>
        protected void Param(Guid template, object value) => Interface(template).Value = value;

        /// <summary>
        /// Changes a parameter of the running logic (like the logic editor)
        /// </summary>
        protected void ChangeParam(Guid template, object value)
        {
            Interface(template).Value = value;
            Sut.ValueChanged(Interface(template), Dispatchable, value);
        }

        protected IList<ILogicOutputChanged> Input(Guid template, object value, IDispatchable source = null)
        {
            var changes = Sut.ValueChanged(Interface(template), source ?? Dispatchable, value);
            foreach (var change in changes)
            {
                Recorder.DispatchValue(change.Instance, change.Value).GetAwaiter().GetResult();
            }
            return changes;
        }

        protected IReadOnlyList<object> Values(Guid output) => Recorder.ValuesOf(OutputId(output));

        protected object Last(Guid output) => Recorder.LastOf(OutputId(output));

        protected void ClearRecorded() => Recorder.ClearValues().GetAwaiter().GetResult();

        /// <summary>
        /// The dispatched id of an output is the id of its interface instance
        /// </summary>
        private Guid OutputId(Guid template) => Interface(template).ObjId;

        protected T Data<T>(string key) => (T)((IDictionary<string, object>)Sut.GetDataForVisu())[key];

        protected System.Timers.Timer SutTimer => (System.Timers.Timer)GetField("_timer");

        protected object GetField(string name)
        {
            for (var t = Sut.GetType(); t != null; t = t.BaseType)
            {
                var field = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(Sut);
                }
            }
            throw new MissingFieldException(Sut.GetType().Name, name);
        }

        protected void FireTick()
        {
            var method = Sut.GetType().GetMethod("_timer_Elapsed", BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? throw new MissingMethodException(Sut.GetType().Name, "_timer_Elapsed");
            method.Invoke(Sut, new object[] { null, null });
        }

        public virtual void Dispose()
        {
            try
            {
                if (_started)
                {
                    Sut.Stop().GetAwaiter().GetResult();
                }
            }
            catch (Exception)
            {
                // must not hide the test's own failure
            }
            GC.SuppressFinalize(this);
        }
    }

    internal sealed class NamedSource : IDispatchable
    {
        public NamedSource(string name, Guid? id = null)
        {
            Name = name;
            Id = id ?? Guid.NewGuid();
        }

        public DispatchableSource Source => DispatchableSource.NodeInstance;
        public DispatchableType Type => DispatchableType.NodeInstance;
        public string Name { get; }
        public Guid Id { get; }
        public bool IsRemanent => false;
    }
}
