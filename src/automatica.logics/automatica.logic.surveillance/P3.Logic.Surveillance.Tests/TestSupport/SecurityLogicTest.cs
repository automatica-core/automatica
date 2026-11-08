using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Surveillance.Security;

namespace P3.Logic.Surveillance.Tests.TestSupport
{
    /// <summary>
    /// A named data point that sends values to the logic (several sources may share one input)
    /// </summary>
    public sealed class TestSource : IDispatchable
    {
        public TestSource(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
        }

        public DispatchableSource Source => DispatchableSource.NodeInstance;
        public DispatchableType Type => DispatchableType.NodeInstance;
        public string Name { get; }
        public Guid Id { get; }
        public bool IsRemanent => false;
    }

    /// <summary>
    /// Runs a security logic against <see cref="FakeTimeProvider"/>, a <see cref="RecordingDispatcher"/> and an in-memory
    /// state store. Ticks are fired by the test (<see cref="Advance"/>), restarts create a new instance on the same store.
    /// </summary>
    public abstract class SecurityLogicTest<TFactory, TLogic> : LogicTest<TFactory>, IDisposable
        where TFactory : LogicFactory
        where TLogic : Automatica.Core.Logic.Logic
    {
        private readonly TestSource _defaultSource = new("Button");
        private bool _started;

        internal RecordingDispatcher Recorder { get; } = new();
        internal MemorySecurityStateStore Store { get; } = new();

        protected TLogic Sut { get; private set; }

        protected static readonly DateTime T0 = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

        protected SecurityLogicTest()
        {
            // the base class starts an instance on the shared dispatcher - not used here
            Logic.Stop().GetAwaiter().GetResult();
            SecurityTimers.Enabled = false;
            FakeTimeProvider.SetDateTime(T0);
            Sut = NewInstance();
        }

        protected abstract TLogic Create(ILogicContext context, ISecurityStateStore store);

        /// <summary>
        /// The controls the logic sees (other logics, fake switches and blinds)
        /// </summary>
        internal FakeControlContext Controls { get; } = new();

        private TLogic NewInstance()
        {
            var context = new LogicContextMock(RuleInstance, Factory, Recorder);
            FakeControlContext.Install(context, Controls);
            return Create(context, Store);
        }

        protected static Automatica.Core.Model.ControlConfiguration ControlsOf(params IControl[] controls) =>
            new() { Controls = controls.Select(a => a.Id).ToList() };

        protected DateTime UtcNow => FakeTimeProvider.GetUtcNow().UtcDateTime;

        /// <summary>
        /// Starts the logic and lets the start-up grace of the pulse inputs pass
        /// </summary>
        protected void Start(bool settle = true)
        {
            Sut.Start().GetAwaiter().GetResult();
            _started = true;
            if (settle)
            {
                Advance(PersistentLogicBase<SecurityStateBase>.StartupGrace.TotalSeconds + 1);
            }
        }

        /// <summary>
        /// Stops the instance and starts a new one on the same state store (a server restart)
        /// </summary>
        protected void Restart(bool settle = true)
        {
            Sut.Stop().GetAwaiter().GetResult();
            Recorder.ClearValues().GetAwaiter().GetResult();
            Sut = NewInstance();
            Start(settle);
        }

        /// <summary>
        /// Moves the clock and runs one tick
        /// </summary>
        protected void Advance(double seconds)
        {
            FakeTimeProvider.SetDateTime(UtcNow.AddSeconds(seconds));
            Tick();
        }

        protected void Tick()
        {
            var method = Sut.GetType().GetMethod("Tick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                         ?? throw new MissingMethodException(Sut.GetType().Name, "Tick");
            method.Invoke(Sut, null);
        }

        /// <summary>
        /// Sets a parameter; on a running logic it is also passed to the logic (as the core does after a change)
        /// </summary>
        protected void SetParam(Guid template, object value)
        {
            var param = GetLogicInterfaceByTemplate(template);
            param.Value = value;
            if (_started)
            {
                typeof(Automatica.Core.Logic.Logic)
                    .GetMethod("ParameterValueChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .Invoke(Sut, new object[] { param, null, value });
            }
        }

        /// <summary>
        /// Sends a value; the outputs the logic returns are recorded like the core would dispatch them
        /// </summary>
        protected IList<ILogicOutputChanged> Input(Guid template, object value, IDispatchable source = null)
        {
            var changes = Sut.ValueChanged(GetLogicInterfaceByTemplate(template), source ?? _defaultSource, value);
            foreach (var change in changes)
            {
                Recorder.DispatchValue(change.Instance, change.Value).GetAwaiter().GetResult();
            }
            return changes;
        }

        /// <summary>
        /// A rising edge and back
        /// </summary>
        protected void Pulse(Guid template, IDispatchable source = null)
        {
            Input(template, true, source);
            Input(template, false, source);
        }

        protected object Last(Guid output) => Recorder.LastOf(GetLogicInterfaceByTemplate(output).ObjId);

        protected IReadOnlyList<object> Values(Guid output) => Recorder.ValuesOf(GetLogicInterfaceByTemplate(output).ObjId);

        protected bool LastBool(Guid output) => Last(output) is bool b && b;

        protected double LastNumber(Guid output) => Convert.ToDouble(Last(output) ?? 0);

        protected void ClearRecorded() => Recorder.ClearValues().GetAwaiter().GetResult();

        protected ISecurityControl Control => (ISecurityControl)Sut;

        protected SecurityCommandResult Command(string command, Dictionary<string, string> args = null, string user = "anna", bool admin = false)
        {
            return Control.ExecuteSecurityCommandAsync(command, args, new SecurityCommandContext(user, admin, FakeTimeProvider.GetUtcNow()))
                .GetAwaiter().GetResult();
        }

        protected Newtonsoft.Json.Linq.JObject VisuState(bool admin = false) =>
            Newtonsoft.Json.Linq.JObject.FromObject(Sut is ISecurityControl c ? c.GetSecurityState(admin) : Sut.GetDataForVisu());

        protected IReadOnlyList<string> ActionCommands(bool admin = false) =>
            VisuState(admin)["actions"]!.Select(a => (string)a["command"]).ToList();

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
                    // a broken logic must not hide the test's own failure
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
