using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control;
using Automatica.Core.Control.Base;
using Automatica.Core.UnitTests.Base.Logics;

namespace P3.Logic.Surveillance.Tests.TestSupport
{
    /// <summary>
    /// <see cref="IControlContext"/> over a dictionary of controls (logics under test, fake switches and blinds)
    /// </summary>
    public sealed class FakeControlContext : IControlContext
    {
        public ConcurrentDictionary<Guid, IControl> Controls { get; } = new();

        public void Add(IControl control) => Controls[control.Id] = control;

        public Task<List<IControl>> GetAsync(List<Guid> controls, CancellationToken cancellationToken = default) =>
            Task.FromResult(controls.Where(Controls.ContainsKey).Select(a => Controls[a]).ToList());

        public Task<IControl> GetAsync(Guid controlId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Controls.TryGetValue(controlId, out var c) ? c : null);

        public Task<List<ISwitch>> GetSwitchesAsync(List<Guid> switches, CancellationToken cancellationToken = default) =>
            Task.FromResult(Controls.Values.OfType<ISwitch>().Where(a => switches.Contains(a.Id)).ToList());

        public Task<ISwitch> GetSwitchAsync(Guid switchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Controls.TryGetValue(switchId, out var c) ? c as ISwitch : null);

        public Task<List<IDimmer>> GetDimmerAsync(List<Guid> dimmer, CancellationToken cancellationToken = default) =>
            Task.FromResult(Controls.Values.OfType<IDimmer>().Where(a => dimmer.Contains(a.Id)).ToList());

        public Task<IDimmer> GetDimmerAsync(Guid dimmerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Controls.TryGetValue(dimmerId, out var c) ? c as IDimmer : null);

        public Task<List<IBlind>> GetBlindsAsync(List<Guid> blinds, CancellationToken cancellationToken = default) =>
            Task.FromResult(Controls.Values.OfType<IBlind>().Where(a => blinds.Contains(a.Id)).ToList());

        public Task<IBlind> GetBlindAsync(Guid blindId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Controls.TryGetValue(blindId, out var c) ? c as IBlind : null);

        /// <summary>
        /// LogicContextMock creates a Moq control context in its constructor, this one replaces it
        /// </summary>
        public static void Install(LogicContextMock context, IControlContext controlContext)
        {
            var field = typeof(LogicContextMock).GetField("<ControlContext>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(nameof(LogicContextMock), "ControlContext");
            field.SetValue(context, controlContext);
        }
    }

    public sealed class FakeSwitch : ISwitch
    {
        public FakeSwitch(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }
        public SwitchState State { get; private set; } = SwitchState.Off;
        public Guid InputId => Guid.Empty;
        public Guid OutputId => Guid.Empty;

        public Task<bool> SwitchAsync(bool state, CancellationToken cancellationToken = default)
        {
            State = state ? SwitchState.On : SwitchState.Off;
            return Task.FromResult(true);
        }

        public Task<bool> SwitchAsync(SwitchState state, CancellationToken cancellationToken = default) => SwitchAsync(state == SwitchState.On, cancellationToken);
        public Task<bool> SwitchOnAsync(CancellationToken cancellationToken = default) => SwitchAsync(true, cancellationToken);
        public Task<bool> SwitchOffAsync(CancellationToken cancellationToken = default) => SwitchAsync(false, cancellationToken);
        public Guid RegisterValueCallback(Action callback) => Guid.NewGuid();
        public void UnregisterValueCallback(Guid id) { }
    }

    public sealed class FakeBlind : IBlind
    {
        public FakeBlind(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }
        public bool IsMoving => false;
        public int Direction => 0;
        public int Position { get; private set; } = 100;
        public int UpCalls { get; private set; }

        public Task StopAsync(CancellationToken token = default) => Task.CompletedTask;

        public Task MoveUpAsync(CancellationToken token = default)
        {
            UpCalls++;
            Position = 0;
            return Task.CompletedTask;
        }

        public Task MoveDownAsync(CancellationToken token = default)
        {
            Position = 100;
            return Task.CompletedTask;
        }

        public Task MoveAbsoluteAsync(int pos, CancellationToken token = default)
        {
            Position = pos;
            return Task.CompletedTask;
        }

        public Guid RegisterValueCallback(Action callback) => Guid.NewGuid();
        public void UnregisterValueCallback(Guid id) { }
    }
}
