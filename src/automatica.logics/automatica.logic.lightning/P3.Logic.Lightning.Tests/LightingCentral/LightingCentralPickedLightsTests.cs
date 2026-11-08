using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Control.Base;
using Automatica.Core.Model;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Lightning.Common;
using P3.Logic.Lightning.LightingCentral;
using P3.Logic.Lightning.LightingController;
using P3.Logic.Lightning.Tests.TestSupport;
using Xunit;
using L = P3.Logic.Lightning.LightingCentral.LightingCentralLogicFactory;

namespace P3.Logic.Lightning.Tests.LightingCentral
{
    /// <summary>
    /// Rooms picked in the "lights" parameter instead of linked
    /// </summary>
    public class LightingCentralPickedLightsTests : LightingLogicTest<LightingCentralLogicFactory>
    {
        private readonly FakeControlContext _controls = new();

        public LightingCentralPickedLightsTests()
        {
            FakeControlContext.Install((LogicContextMock)Sut.Context, _controls);
        }

        private LightingControllerLogic AddController(string name)
        {
            var factory = new LightingControllerLogicFactory();
            var templates = new LogicTemplateFactoryMock();
            factory.InitTemplates(templates);
            var instance = templates.CreateRuleInstanceFromTemplate(factory.LogicGuid);
            instance.Name = name;
            // the mock gives every instance the template id
            instance.ObjId = Guid.NewGuid();
            var controller = (LightingControllerLogic)factory.CreateLogicInstance(new LogicContextMock(instance, templates, Recorder));
            controller.TimerEnabled = false;
            controller.Start().GetAwaiter().GetResult();
            _controls.Add(controller);
            return controller;
        }

        private FakeSwitch AddSwitch(string name, bool dimmer = false)
        {
            var light = dimmer ? new FakeDimmer(name) : new FakeSwitch(name);
            _controls.Add(light);
            return light;
        }

        private void Pick(params IControl[] controls)
        {
            SetParam(L.ParamLights, new ControlConfiguration { Controls = controls.Select(a => a.Id).ToList() });
        }

        [Fact]
        public void On_SwitchesPickedControllersAndLightsOn_OthersStayOff()
        {
            var living = AddController("Living");
            var kitchen = AddController("Kitchen");
            var other = AddController("Bedroom");
            var hall = AddSwitch("Hall");
            var stairs = (FakeDimmer)AddSwitch("Stairs", true);
            Pick(living, kitchen, hall, stairs);
            Start();

            Pulse(L.On);

            Assert.Equal(Mood.AllOn, living.CurrentMood);
            Assert.Equal(Mood.AllOn, kitchen.CurrentMood);
            Assert.Equal(Mood.AllOff, other.CurrentMood);
            Assert.Equal(SwitchState.On, hall.State);
            Assert.Equal(100, stairs.DimmerValue);
            Assert.Equal(4, Last(L.ActiveCount));
            Assert.Equal(true, Last(L.AnyActive));
            // linked controllers still get the pulse
            Assert.Equal(new object[] { true, false }, Values(L.OnOut));
        }

        [Fact]
        public void Off_SwitchesEverythingOff()
        {
            var living = AddController("Living");
            var hall = AddSwitch("Hall");
            Pick(living, hall);
            Start();
            Pulse(L.On);

            Pulse(L.Off);

            Assert.Equal(Mood.AllOff, living.CurrentMood);
            Assert.False(living.AnyLightOn);
            Assert.Equal(SwitchState.Off, hall.State);
            Assert.Equal(0, Last(L.ActiveCount));
            Assert.Equal(false, Last(L.AnyActive));
        }

        [Fact]
        public void MoodAndSteps_ReachOnlyTheControllers()
        {
            var living = AddController("Living");
            var hall = AddSwitch("Hall");
            Pick(living, hall);
            Start();

            Input(L.Mood, Mood.AllOn);
            Assert.Equal(Mood.AllOn, living.CurrentMood);
            Assert.Equal(SwitchState.On, hall.State);

            Input(L.Mood, 0);
            Assert.Equal(Mood.AllOff, living.CurrentMood);
            Assert.Equal(SwitchState.Off, hall.State);

            // the default controller has no user moods: + goes to "all on"
            Pulse(L.MoodPlus);
            Assert.Equal(Mood.AllOn, living.CurrentMood);
            Assert.Equal(SwitchState.Off, hall.State);
        }

        [Fact]
        public void LinkedAndPickedRooms_AreCountedTogether_AndChangesArePolled()
        {
            var living = AddController("Living");
            Pick(living);
            Start();
            Input(L.Active[0], true);
            Assert.Equal(1, Last(L.ActiveCount));

            // switched on at the controller itself: the central sees it on its next poll
            Input(L.Mood, Mood.AllOn);
            Assert.Equal(2, Last(L.ActiveCount));
            living.ValueChanged(living.Context.RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == living.Ids.Command), null, "off");
            Assert.Equal(2, Last(L.ActiveCount));
            Assert.NotNull(Sut.NextTick);
            AdvanceSeconds(LightingCentralLogic.PollInterval.TotalSeconds);
            Assert.Equal(1, Last(L.ActiveCount));
        }

        [Fact]
        public void NothingPicked_NoPolling()
        {
            Start();
            Assert.Null(Sut.NextTick);
        }

        [Fact]
        public void PickedControllerIsAControl()
        {
            var living = AddController("Living");
            Assert.Equal(living.Context.RuleInstance.ObjId, living.Id);
            Assert.Equal("Living", living.Name);
        }

        [Fact]
        public void ParameterAsJson_IsRead()
        {
            var id = Guid.NewGuid();
            Assert.Equal(new List<Guid> { id }, CentralTargets.Ids($"{{\"Controls\":[\"{id}\"]}}"));
            Assert.Empty(CentralTargets.Ids(null));
            Assert.Empty(CentralTargets.Ids("not json"));
        }
    }
}
