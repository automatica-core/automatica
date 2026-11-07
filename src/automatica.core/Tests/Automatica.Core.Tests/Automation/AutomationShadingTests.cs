using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Engine;
using Automatica.Core.EF.Models.Automations;
using Automatica.Core.Tests.Shading;
using Xunit;
using static Automatica.Core.Tests.Automation.AutomationTestHelper;

namespace Automatica.Core.Tests.Automation
{
    /// <summary>
    /// The "shading" action and condition of the automations
    /// </summary>
    public class AutomationShadingTests
    {
        private readonly FakeAutomationEnvironment _environment;
        private readonly InMemoryAutomationStore _store = new();
        private readonly AutomationEngine _engine;
        private readonly Guid _button;
        private readonly FakeBlind _blind = new() { Name = "Living room" };
        private readonly FakeBlind _other = new() { Name = "Kitchen" };

        public AutomationShadingTests()
        {
            _environment = new FakeAutomationEnvironment(CreateTime());
            _engine = CreateEngine(_environment, _store);
            _button = _environment.AddNode("Button", value: false);
            _environment.Controls[_blind.Id] = _blind;
            _environment.Controls[_other.Id] = _other;
            _environment.Shading[_blind.Id] = false;
        }

        private AutomationDefinition OnButton(ConditionDefinition condition, params ActionDefinition[] actions)
        {
            var definition = new AutomationDefinition
            {
                Triggers = { new DatapointTrigger { Id = "button", Node = _button, Change = DatapointChange.To, Value = Json(true) } },
                Actions = actions.ToList()
            };
            if (condition != null)
            {
                definition.Conditions.Conditions.Add(condition);
            }
            return definition;
        }

        [Fact]
        public async Task ShadingAction_PausesTheBlinds()
        {
            _store.Add(OnButton(null, new ShadingAction { Controls = { _blind.Id }, Enable = false }));
            await _engine.StartAsync();

            await _environment.PublishAsync(_button, true);
            await Eventually(() => _store.Runs.Count == 1, "run stored");

            Assert.Equal(AutomationRunState.Done, _store.Runs.Single().State);
            Assert.False(_environment.ShadingEnabled[_blind.Id]);
        }

        [Fact]
        public async Task ShadingAction_ByArea_TakesAllBlinds_SkipsTheOnesWithoutShading()
        {
            _store.Add(OnButton(null, new ShadingAction { Area = Guid.NewGuid(), Enable = true }));
            await _engine.StartAsync();

            await _environment.PublishAsync(_button, true);
            await Eventually(() => _store.Runs.Count == 1, "run stored");

            Assert.True(_environment.ShadingEnabled[_blind.Id]);
            Assert.False(_environment.ShadingEnabled.ContainsKey(_other.Id));
            Assert.Contains("\"value\":1", _store.Runs.Single().Steps);
        }

        [Fact]
        public async Task ShadingCondition_ChecksTheCurrentState()
        {
            var light = _environment.AddNode("Light");
            _store.Add(OnButton(new ShadingCondition { Control = _blind.Id, Shading = true }, new SetValueAction { Node = light, Value = Json(true) }));
            await _engine.StartAsync();

            await _environment.PublishAsync(_button, true);
            await Eventually(() => _store.Runs.Count == 1, "first run stored");
            Assert.Equal(AutomationRunState.ConditionsNotMet, _store.Runs.Last().State);

            _environment.Shading[_blind.Id] = true;
            await _environment.PublishAsync(_button, false);
            await _environment.PublishAsync(_button, true);
            await Eventually(() => _store.Runs.Count == 2, "second run stored");
            Assert.Equal(AutomationRunState.Done, _store.Runs.Last().State);
        }

        [Fact]
        public async Task Validation()
        {
            var problems = await _engine.ValidateAsync(OnButton(new ShadingCondition { Control = _other.Id }, new ShadingAction()), null);

            Assert.Contains(problems, a => a.Code == "groupTargetMissing" && a.Severity == ValidationSeverity.Error);
            Assert.Contains(problems, a => a.Code == "shadingNotConfigured" && a.Severity == ValidationSeverity.Warning);

            var switchOnly = new FakeSwitch();
            _environment.Controls[switchOnly.Id] = switchOnly;
            problems = await _engine.ValidateAsync(OnButton(null, new ShadingAction { Controls = { switchOnly.Id } }), null);
            Assert.Contains(problems, a => a.Code == "controlNotSupported");
        }

        [Fact]
        public void Definition_RoundTrip()
        {
            var definition = OnButton(new ShadingCondition { Control = _blind.Id, Shading = false }, new ShadingAction { Controls = { _blind.Id }, Enable = false });
            var json = AutomationJson.Serialize(definition);
            Assert.Contains("\"type\":\"shading\"", json);

            var parsed = AutomationJson.Parse(json);
            var condition = Assert.IsType<ShadingCondition>(parsed.Conditions.Conditions.Single());
            Assert.False(condition.Shading);
            var action = Assert.IsType<ShadingAction>(parsed.Actions.Single());
            Assert.False(action.Enable);
            Assert.Equal(_blind.Id, action.Controls.Single());
        }
    }
}
