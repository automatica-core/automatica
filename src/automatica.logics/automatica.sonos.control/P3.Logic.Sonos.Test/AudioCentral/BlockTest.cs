using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;

namespace P3.Logic.Sonos.Tests.AudioCentral
{
    /// <summary>
    /// A clock owned by one test. The shared FakeTimeProvider is reset by the constructor of every LogicTest, which runs in
    /// parallel in other test classes, so time based tests use their own clock.
    /// </summary>
    internal sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        private TimeZoneInfo _zone = TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => _zone;

        public void Set(DateTimeOffset utc) => _now = utc.ToUniversalTime();
        public void Advance(double seconds) => _now = _now.AddSeconds(seconds);
        public void SetZone(TimeZoneInfo zone) => _zone = zone;
    }

    /// <summary>
    /// <see cref="LogicContextMock"/> with the test's own clock
    /// </summary>
    internal sealed class ClockLogicContext : LogicContextMock, ILogicContext
    {
        private readonly TimeProvider _clock;

        public ClockLogicContext(RuleInstance instance, ILogicTemplateFactory factory, IDispatcher dispatcher, TimeProvider clock)
            : base(instance, factory, dispatcher)
        {
            _clock = clock;
        }

        TimeProvider ILogicContext.TimeProvider => _clock;
    }

    /// <summary>
    /// A value that comes from the visualization
    /// </summary>
    internal sealed class VisuSource : IDispatchable
    {
        public static VisuSource Instance { get; } = new();
        public DispatchableSource Source => DispatchableSource.Visualization;
        public DispatchableType Type => DispatchableType.Visualization;
        public string Name => "Visu";
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsRemanent => false;
    }

    /// <summary>
    /// A value from a link (node or other logic). The DispatchableMock of the test base claims to be the visualization.
    /// </summary>
    internal sealed class LinkSource : IDispatchable
    {
        public static LinkSource Instance { get; } = new();
        public DispatchableSource Source => DispatchableSource.NodeInstance;
        public DispatchableType Type => DispatchableType.NodeInstance;
        public string Name => "Link";
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsRemanent => false;
    }

    /// <summary>
    /// Records every dispatched value synchronously, in call order, by dispatchable id
    /// </summary>
    internal sealed class BlockRecorder : IDispatcher
    {
        private readonly object _lock = new();
        private readonly List<(Guid Id, object Value)> _dispatched = new();
        private readonly Dictionary<Guid, DispatchValue> _values = new();

        public IReadOnlyList<object> ValuesOf(Guid id)
        {
            lock (_lock)
            {
                return _dispatched.Where(a => a.Id == id).Select(a => a.Value).ToArray();
            }
        }

        public IReadOnlyList<(Guid Id, object Value)> All()
        {
            lock (_lock)
            {
                return _dispatched.ToArray();
            }
        }

        public Task Init(CancellationToken token = default) => Task.CompletedTask;

        public Task DispatchValue(IDispatchable self, object value, DispatchValueSource valueSource = DispatchValueSource.Read)
        {
            return DispatchValue(self, new DispatchValue(self.Id, self.Type, value, DateTime.UnixEpoch, valueSource));
        }

        public Task DispatchValue(IDispatchable self, DispatchValue value)
        {
            lock (_lock)
            {
                _dispatched.Add((self.Id, value.Value));
                _values[self.Id] = value;
            }
            return Task.CompletedTask;
        }

        public Task UnRegisterDispatch(DispatchableType type, Guid id) => Task.CompletedTask;

        public Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback) => Task.CompletedTask;

        public IDictionary<Guid, DispatchValue> GetValues()
        {
            lock (_lock)
            {
                return new Dictionary<Guid, DispatchValue>(_values);
            }
        }

        public IDictionary<Guid, DispatchValue> GetValues(DispatchableType type) => GetValues();

        public DispatchValue GetValue(Guid id)
        {
            lock (_lock)
            {
                return _values.TryGetValue(id, out var value) ? value : null;
            }
        }

        public DispatchValue GetValue(DispatchableType type, Guid id) => GetValue(id);

        public Task ClearRegistrations() => Task.CompletedTask;

        public Task ClearValues()
        {
            lock (_lock)
            {
                _dispatched.Clear();
                _values.Clear();
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Runs a block against its own clock and a recording dispatcher; timer ticks are fired by the test.
    /// </summary>
    public abstract class BlockTest<TFactory> : LogicTest<TFactory>, IDisposable where TFactory : LogicFactory
    {
        internal BlockRecorder Recorder { get; } = new();
        internal ManualClock Clock { get; } = new();
        protected ILogic Sut { get; }
        private bool _started;

        protected BlockTest()
        {
            // the base class starts an instance on the shared dispatcher - it is not used here
            Logic.Stop().GetAwaiter().GetResult();
            Sut = Instance.CreateLogicInstance(new ClockLogicContext(RuleInstance, Factory, Recorder, Clock));
        }

        protected RuleInterfaceInstance Interface(Guid template) => GetLogicInterfaceByTemplate(template);

        /// <summary>
        /// Sets a parameter before <see cref="Start"/>
        /// </summary>
        protected void Param(Guid template, object value) => Interface(template).Value = value;

        /// <summary>
        /// Changes a parameter of the running block
        /// </summary>
        protected void ChangeParam(Guid template, object value) => Sut.ValueChanged(Interface(template), Dispatchable, value);

        protected void Start()
        {
            Sut.Start().GetAwaiter().GetResult();
            _started = true;
        }

        protected void Input(Guid template, object value) => Sut.ValueChanged(Interface(template), LinkSource.Instance, value);

        protected void VisuInput(Guid template, object value) => Sut.ValueChanged(Interface(template), VisuSource.Instance, value);

        protected IReadOnlyList<object> Values(Guid output) => Recorder.ValuesOf(output);

        protected object Last(Guid output)
        {
            var values = Values(output);
            return values.Count == 0 ? null : values[^1];
        }

        protected void ClearRecorded() => Recorder.ClearValues().GetAwaiter().GetResult();

        protected T Field<T>(string name)
        {
            for (var t = Sut.GetType(); t != null; t = t.BaseType)
            {
                var field = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    return (T)field.GetValue(Sut);
                }
            }
            throw new MissingFieldException(Sut.GetType().Name, name);
        }

        protected System.Timers.Timer Timer(string name = "_timer") => Field<System.Timers.Timer>(name);

        /// <summary>
        /// Runs a timer handler (e.g. "_timer_Elapsed") synchronously, as if the timer had elapsed
        /// </summary>
        protected void Fire(string handler = "_timer_Elapsed")
        {
            var method = Sut.GetType().GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? throw new MissingMethodException(Sut.GetType().Name, handler);
            method.Invoke(Sut, new object[] { null, null });
        }

        protected IDictionary<string, object> VisuData => (IDictionary<string, object>)Sut.GetDataForVisu();

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
                // a test that leaves the block broken must not hide its own failure
            }
            GC.SuppressFinalize(this);
        }
    }
}
