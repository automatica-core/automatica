using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control;
using Automatica.Core.Control.Base;
using Automatica.Core.Model;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Garage;
using P3.Logic.Operations.WindowCentral;
using Xunit;
using G = P3.Logic.Operations.Garage.GarageCentralLogicFactory;
using W = P3.Logic.Operations.WindowCentral.WindowCentralLogicFactory;

namespace P3.Logic.Operations.Tests.GarageMedia
{
    /// <summary>
    /// <see cref="IControlContext"/> over a dictionary of controls (logics under test, fake blinds)
    /// </summary>
    internal sealed class FakeControlContext : IControlContext
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

    /// <summary>
    /// A blind that jumps to its target. Position 0 is up (window open), 100 down (closed).
    /// </summary>
    internal sealed class FakeBlind : IBlind
    {
        public FakeBlind(string name, int position = 100)
        {
            Id = Guid.NewGuid();
            Name = name;
            Position = position;
        }

        public Guid Id { get; }
        public string Name { get; }
        public bool IsMoving => false;
        public int Direction => 0;
        public int Position { get; set; }
        public List<string> Calls { get; } = new();

        public Task StopAsync(CancellationToken token = default)
        {
            Calls.Add("stop");
            return Task.CompletedTask;
        }

        public Task MoveUpAsync(CancellationToken token = default)
        {
            Calls.Add("up");
            Position = 0;
            return Task.CompletedTask;
        }

        public Task MoveDownAsync(CancellationToken token = default)
        {
            Calls.Add("down");
            Position = 100;
            return Task.CompletedTask;
        }

        public Task MoveAbsoluteAsync(int pos, CancellationToken token = default)
        {
            Calls.Add($"abs:{pos}");
            Position = pos;
            return Task.CompletedTask;
        }

        public Guid RegisterValueCallback(Action callback) => Guid.NewGuid();
        public void UnregisterValueCallback(Guid id) { }
    }

    public class GarageCentralPickedGatesTests : BlockTest<GarageCentralLogicFactory>
    {
        private readonly FakeControlContext _controls = new();

        public GarageCentralPickedGatesTests()
        {
            FakeControlContext.Install((LogicContextMock)((Automatica.Core.Logic.Logic)Sut).Context, _controls);
        }

        private GarageCentralLogic Central => (GarageCentralLogic)Sut;

        private GarageGateLogic AddGate(string name)
        {
            var factory = new GarageGateLogicFactory();
            var templates = new LogicTemplateFactoryMock();
            factory.InitTemplates(templates);
            var instance = templates.CreateRuleInstanceFromTemplate(factory.LogicGuid);
            instance.Name = name;
            // the mock gives every instance the template id
            instance.ObjId = Guid.NewGuid();
            var gate = (GarageGateLogic)factory.CreateLogicInstance(new ClockLogicContext(instance, templates, new BlockRecorder(), Clock));
            gate.Start().GetAwaiter().GetResult();
            _controls.Add(gate);
            return gate;
        }

        private void Pick(params IControl[] controls) =>
            ChangeParam(G.ParamGates, new ControlConfiguration { Controls = controls.Select(a => a.Id).ToList() });

        [Fact]
        public void Factory_HasTheGatesParameter()
        {
            Assert.NotNull(Interface(G.ParamGates));
            Assert.Equal(new Version(1, 1, 0, 0), new GarageCentralLogicFactory().LogicVersion);
        }

        [Fact]
        public void Open_ReachesOnlyThePickedGates_AndTheyAreCounted()
        {
            var left = AddGate("Left");
            var right = AddGate("Right");
            var other = AddGate("Shed");
            Start();
            Pick(left, right);
            Assert.Equal(2, Last(G.OutputClosedCount));

            Input(G.InputCompleteOpen, true);

            Assert.Equal(GarageState.Opening, left.State);
            Assert.Equal(GarageState.Opening, right.State);
            Assert.Equal(GarageState.Closed, other.State);
            Assert.Equal(2, Last(G.OutputOpenCount));
            Assert.Equal(0, Last(G.OutputClosedCount));
            // linked blocks still get the command
            Assert.Equal((int)GarageCommand.Open, Last(G.OutputCommand));
        }

        [Fact]
        public void Toggle_UsesTheStateOfThePickedGates()
        {
            var gate = AddGate("Gate");
            Start();
            Pick(gate);

            Input(G.InputToggle, true);
            Assert.Equal(GarageState.Opening, gate.State);

            // moving: the next toggle stops
            Clock.Advance(5);
            Input(G.InputToggle, true);
            Assert.Equal((int)GarageCommand.Stop, Last(G.OutputCommand));
            Assert.Equal(GarageState.PartiallyOpen, gate.State);
        }

        [Fact]
        public void Poll_PicksUpChangesMadeAtTheGate()
        {
            var gate = AddGate("Gate");
            Start();
            Pick(gate);
            Assert.Equal(0, Last(G.OutputOpenCount));

            gate.ValueChanged(gate.Context.RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == GarageGateLogicFactory.InputOpen),
                LinkSource.Instance, true);
            Assert.Equal(0, Last(G.OutputOpenCount));

            Central.Poll();
            Assert.Equal(1, Last(G.OutputOpenCount));
        }

        [Fact]
        public void Lock_StopsThePickedGates()
        {
            var gate = AddGate("Gate");
            Start();
            Pick(gate);
            Input(G.InputCompleteOpen, true);
            Assert.NotEqual(0, gate.Motion);

            Input(G.InputLock, true);
            Assert.Equal(0, gate.Motion);
        }
    }

    public class WindowCentralPickedWindowsTests : BlockTest<WindowCentralLogicFactory>
    {
        private readonly FakeControlContext _controls = new();

        public WindowCentralPickedWindowsTests()
        {
            FakeControlContext.Install((LogicContextMock)((Automatica.Core.Logic.Logic)Sut).Context, _controls);
        }

        private WindowCentralLogic Central => (WindowCentralLogic)Sut;

        private FakeBlind AddWindow(string name, int position = 100)
        {
            var window = new FakeBlind(name, position);
            _controls.Add(window);
            return window;
        }

        private void Pick(params IControl[] controls) =>
            ChangeParam(W.ParamWindows, new ControlConfiguration { Controls = controls.Select(a => a.Id).ToList() });

        [Fact]
        public void Factory_HasTheWindowsParameter()
        {
            Assert.NotNull(Interface(W.ParamWindows));
            Assert.Equal(new Version(1, 1, 0, 0), new WindowCentralLogicFactory().LogicVersion);
        }

        [Fact]
        public void OpenCloseStop_MoveThePickedWindows()
        {
            var kitchen = AddWindow("Kitchen");
            var bath = AddWindow("Bath");
            var other = AddWindow("Attic");
            Start();
            Pick(kitchen, bath);
            Assert.Equal(2, Last(W.OutputClosedCount));

            Input(W.InputCompleteOpen, true);
            Assert.Equal(new[] { "up" }, kitchen.Calls);
            Assert.Equal(new[] { "up" }, bath.Calls);
            Assert.Empty(other.Calls);

            Central.Poll();
            Assert.Equal(2, Last(W.OutputOpenCount));
            Assert.Equal(0, Last(W.OutputClosedCount));

            Input(W.InputCompleteClose, true);
            Input(W.InputLock, true);
            Assert.Equal(new[] { "up", "down", "stop" }, kitchen.Calls);
        }

        [Fact]
        public void Position_And_SlightlyOpen_AreSentAsBlindPositions()
        {
            Param(W.ParamSlightlyOpen, 15);
            var window = AddWindow("Kitchen");
            Start();
            Pick(window);

            Input(W.InputPosition, 30);
            Input(W.InputSlightlyOpen, true);

            Assert.Equal(new[] { "abs:70", "abs:85" }, window.Calls);
            Assert.Equal(new object[] { 30.0, 15.0 }, Values(W.OutputPosition));
        }

        [Fact]
        public void Toggle_ClosesWhenAPickedWindowIsOpen()
        {
            AddWindow("Closed");
            var open = AddWindow("Open", 0);
            Start();
            Pick(_controls.Controls.Values.ToArray());
            Assert.Equal(1, Last(W.OutputOpenCount));

            Input(W.InputToggle, true);
            Assert.Equal(new[] { "down" }, open.Calls);
        }

        [Fact]
        public void WeatherProtection_ClosesThePickedWindows()
        {
            var window = AddWindow("Kitchen", 0);
            Start();
            Pick(window);

            Input(W.InputWeatherProtection, true);
            Assert.Equal(new[] { "down" }, window.Calls);
            Input(W.InputCompleteOpen, true);
            Assert.Equal(new[] { "down" }, window.Calls);
        }
    }
}
