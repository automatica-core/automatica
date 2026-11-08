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
using P3.Logic.Sonos.AudioCentral;
using P3.Logic.Sonos.SonosControl;
using Xunit;
using F = P3.Logic.Sonos.AudioCentral.AudioCentralLogicFactory;
using S = P3.Logic.Sonos.SonosControl.SonosControlLogicFactory;

namespace P3.Logic.Sonos.Tests.AudioCentral
{
    /// <summary>
    /// <see cref="IControlContext"/> over a dictionary of controls (the player blocks under test)
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
            Task.FromResult(new List<ISwitch>());

        public Task<ISwitch> GetSwitchAsync(Guid switchId, CancellationToken cancellationToken = default) => Task.FromResult<ISwitch>(null);

        public Task<List<IDimmer>> GetDimmerAsync(List<Guid> dimmer, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<IDimmer>());

        public Task<IDimmer> GetDimmerAsync(Guid dimmerId, CancellationToken cancellationToken = default) => Task.FromResult<IDimmer>(null);

        public Task<List<IBlind>> GetBlindsAsync(List<Guid> blinds, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<IBlind>());

        public Task<IBlind> GetBlindAsync(Guid blindId, CancellationToken cancellationToken = default) => Task.FromResult<IBlind>(null);

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

    public class AudioCentralPickedPlayersTests : BlockTest<F>
    {
        private readonly FakeControlContext _controls = new();

        public AudioCentralPickedPlayersTests()
        {
            FakeControlContext.Install((LogicContextMock)((Automatica.Core.Logic.Logic)Sut).Context, _controls);
        }

        private SonosControlLogic AddPlayer(string name, long volume = 0)
        {
            var factory = new SonosControlLogicFactory();
            var templates = new LogicTemplateFactoryMock();
            factory.InitTemplates(templates);
            var instance = templates.CreateRuleInstanceFromTemplate(factory.LogicGuid);
            instance.Name = name;
            // the mock gives every instance the template id
            instance.ObjId = Guid.NewGuid();
            var player = (SonosControlLogic)factory.CreateLogicInstance(new ClockLogicContext(instance, templates, new BlockRecorder(), Clock));
            player.Start().GetAwaiter().GetResult();
            if (volume > 0)
            {
                PlayerInput(player, S.Volume, volume);
            }
            _controls.Add(player);
            return player;
        }

        private static void PlayerInput(SonosControlLogic player, Guid template, object value) =>
            player.ValueChanged(player.Context.RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == template), LinkSource.Instance, value);

        private void Pick(params IControl[] controls) =>
            ChangeParam(F.ParamPlayers, new ControlConfiguration { Controls = controls.Select(a => a.Id).ToList() });

        private void Poll() => typeof(AudioCentralLogic).GetMethod("Poll", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Sut, null);

        [Fact]
        public void Factory_HasThePlayersParameter()
        {
            Assert.NotNull(Interface(F.ParamPlayers));
            Assert.Equal(new Version(1, 2, 0, 0), new F().LogicVersion);
        }

        [Fact]
        public void OnOff_ReachOnlyThePickedPlayers_AndTheyAreCounted()
        {
            var kitchen = AddPlayer("Kitchen");
            var living = AddPlayer("Living");
            var other = AddPlayer("Bath");
            Start();
            Pick(kitchen, living);

            Input(F.InputOn, true);
            Assert.True(kitchen.IsPlaying);
            Assert.True(living.IsPlaying);
            Assert.False(other.IsPlaying);
            Assert.Equal(2, Last(F.OutputActiveCount));

            Input(F.InputOff, true);
            Assert.False(kitchen.IsPlaying);
            Assert.Equal(0, Last(F.OutputActiveCount));
        }

        [Fact]
        public void Toggle_UsesThePickedPlayers()
        {
            var kitchen = AddPlayer("Kitchen");
            Start();
            Pick(kitchen);
            PlayerInput(kitchen, S.PlayPauseTrigger, true);

            Input(F.InputToggle, true);
            Assert.False(kitchen.IsPlaying);
            Assert.Equal(false, Last(F.OutputPlay));
        }

        [Fact]
        public void Volume_KeepsTheRatioOfThePickedPlayers()
        {
            var loud = AddPlayer("Loud", 40);
            var quiet = AddPlayer("Quiet", 20);
            Start();
            Pick(loud, quiet);

            Input(F.InputVolume, 60);

            Assert.Equal(60, loud.CurrentVolume);
            Assert.Equal(30, quiet.CurrentVolume);
        }

        [Fact]
        public void VolumeUp_ReachesThePlayingPickedPlayers()
        {
            var kitchen = AddPlayer("Kitchen", 10);
            Start();
            Pick(kitchen);
            Input(F.InputOn, true);

            Input(F.InputVolumeUp, true);
            Assert.Equal(11, kitchen.CurrentVolume);
        }

        [Fact]
        public void Poll_PicksUpPlayersStartedElsewhere()
        {
            var kitchen = AddPlayer("Kitchen");
            Start();
            Pick(kitchen);
            Assert.Equal(0, Last(F.OutputActiveCount));

            PlayerInput(kitchen, S.PlayPauseTrigger, true);
            Poll();
            Assert.Equal(1, Last(F.OutputActiveCount));
        }

        [Fact]
        public void PlayerIsAControl()
        {
            var kitchen = AddPlayer("Kitchen");
            Assert.Equal(kitchen.Context.RuleInstance.ObjId, kitchen.Id);
            Assert.Equal("Kitchen", kitchen.Name);
        }
    }
}
