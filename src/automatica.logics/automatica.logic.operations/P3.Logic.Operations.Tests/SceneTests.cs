using System;
using System.Linq;
using System.Reflection;
using Automatica.Core.Base.Templates;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Scenes;
using P3.Logic.Operations.Tests.Support;
using Xunit;

namespace P3.Logic.Operations.Tests
{
    /// <summary>
    /// Scene block: Act pulses Q (the automation trigger "scene" listens to it), Off locks it. Pulse ends are fired by the test.
    /// </summary>
    public class SceneTests : LogicTest<SceneLogicFactory>, IDisposable
    {
        private readonly RecordingDispatcher _recorder = new();
        private readonly Scene _sut;

        public SceneTests()
        {
            // the base class starts an instance that dispatches into the shared dispatcher - it is not used here
            Logic.Stop().GetAwaiter().GetResult();
            _sut = (Scene)Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, _recorder));
            _sut.Start().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _sut.Stop().GetAwaiter().GetResult();
        }

        private Guid ActivatedId => GetLogicInterfaceByTemplate(SceneLogicFactory.RuleActivated).ObjId;

        /// <summary>
        /// Values of Q after the 0 sent by the start
        /// </summary>
        private object[] Pulses => _recorder.ValuesOf(ActivatedId).Skip(1).ToArray();

        private System.Collections.Generic.IList<ILogicOutputChanged> Input(Guid template, object value)
            => _sut.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        private void EndPulse()
        {
            typeof(Scene).GetMethod("_timer_Elapsed", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, new object[] { null, null });
        }

        [Fact]
        public void Template_HasPushButtonInterfacesAndActivatedKey()
        {
            var activate = GetLogicInterfaceByTemplate(SceneLogicFactory.RuleActivate);
            var activated = GetLogicInterfaceByTemplate(SceneLogicFactory.RuleActivated);

            // the visualization push button writes into the interface of type Input
            Assert.Equal(RuleInterfaceType.Input, activate.This2RuleInterfaceTemplateNavigation.InterfaceType);
            Assert.Equal((long)LogicInterfaceDirection.Output, activated.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection);
            Assert.Equal(SceneLogicFactory.ActivatedKey, activated.This2RuleInterfaceTemplateNavigation.Key);
            Assert.Equal(4, RuleInstance.RuleInterfaceInstance.Count);
        }

        [Fact]
        public void IsAControl()
        {
            IControl control = _sut;
            Assert.Equal(RuleInstance.ObjId, control.Id);
            Assert.Equal(RuleInstance.Name, control.Name);
            // the controls API names unknown controls by class name, the automation editor filters on "scene"
            Assert.Equal("scene", _sut.GetType().Name.ToLowerInvariant());
        }

        [Fact]
        public void Start_SendsZero()
        {
            Assert.Equal(new object[] { false }, _recorder.ValuesOf(ActivatedId).ToArray());
        }

        [Fact]
        public void Activate_PulsesTrueThenFalse()
        {
            Assert.Empty(Input(SceneLogicFactory.RuleActivate, true));
            Assert.Equal(new object[] { true }, Pulses);

            EndPulse();
            Assert.Equal(new object[] { true, false }, Pulses);
            Assert.Equal(1, _sut.Activations);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData(0.0)]
        [InlineData("0")]
        [InlineData("false")]
        [InlineData(null)]
        public void FalsyValues_DoNotActivate(object value)
        {
            Input(SceneLogicFactory.RuleActivate, value);
            Assert.Empty(Pulses);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2.5)]
        [InlineData("1")]
        [InlineData("true")]
        public void TruthyValues_Activate(object value)
        {
            Input(SceneLogicFactory.RuleActivate, value);
            Assert.Equal(new object[] { true }, Pulses);
        }

        [Fact]
        public void RepeatedTrue_FromTheVisu_ActivatesEveryTime()
        {
            // the visualization button sends true on every press, never false
            Input(SceneLogicFactory.RuleActivate, true);
            EndPulse();
            Input(SceneLogicFactory.RuleActivate, true);
            EndPulse();

            Assert.Equal(new object[] { true, false, true, false }, Pulses);
            Assert.Equal(2, _sut.Activations);
        }

        [Fact]
        public void ActivationDuringThePulse_SendsANewEdge()
        {
            Input(SceneLogicFactory.RuleActivate, true);
            Input(SceneLogicFactory.RuleActivate, true);
            EndPulse();

            Assert.Equal(new object[] { true, false, true, false }, Pulses);
        }

        [Fact]
        public void LateTick_AfterThePulseEnded_DoesNothing()
        {
            Input(SceneLogicFactory.RuleActivate, true);
            EndPulse();
            EndPulse();
            Assert.Equal(new object[] { true, false }, Pulses);
        }

        [Fact]
        public void Off_LocksTheScene()
        {
            var lockedOutputs = Input(SceneLogicFactory.RuleOff, true);
            Assert.Equal(true, Assert.Single(lockedOutputs).Value);
            Assert.True(_sut.IsLocked);

            Input(SceneLogicFactory.RuleActivate, true);
            Assert.Empty(Pulses);

            // repeating the same lock state reports nothing
            Assert.Empty(Input(SceneLogicFactory.RuleOff, 1));

            var unlocked = Input(SceneLogicFactory.RuleOff, false);
            Assert.Equal(false, Assert.Single(unlocked).Value);

            Input(SceneLogicFactory.RuleActivate, true);
            Assert.Equal(new object[] { true }, Pulses);
        }

        [Fact]
        public void Stopped_IgnoresActivation()
        {
            _sut.Stop().GetAwaiter().GetResult();
            Input(SceneLogicFactory.RuleActivate, true);
            Assert.Empty(Pulses);
            _sut.Start().GetAwaiter().GetResult();
        }
    }
}
