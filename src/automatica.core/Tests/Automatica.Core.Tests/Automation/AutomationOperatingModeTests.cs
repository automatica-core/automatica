using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Engine;
using Automatica.Core.EF.Models.Automations;
using Xunit;
using static Automatica.Core.Tests.Automation.AutomationTestHelper;

namespace Automatica.Core.Tests.Automation
{
    /// <summary>
    /// The "operating mode" condition of the automations
    /// </summary>
    public class AutomationOperatingModeTests
    {
        private readonly FakeAutomationEnvironment _environment;
        private readonly InMemoryAutomationStore _store = new();
        private readonly AutomationEngine _engine;
        private readonly Guid _button;
        private readonly Guid _light;

        public AutomationOperatingModeTests()
        {
            _environment = new FakeAutomationEnvironment(CreateTime());
            _engine = CreateEngine(_environment, _store);
            _button = _environment.AddNode("Button", value: false);
            _light = _environment.AddNode("Light");
            _environment.OperatingModes["vacation"] = false;
        }

        private AutomationDefinition OnButton(ConditionDefinition condition)
        {
            var definition = new AutomationDefinition
            {
                Triggers = { new DatapointTrigger { Id = "button", Node = _button, Change = DatapointChange.To, Value = Json(true) } },
                Actions = { new SetValueAction { Node = _light, Value = Json(true) } }
            };
            definition.Conditions.Conditions.Add(condition);
            return definition;
        }

        [Fact]
        public async Task Condition_ChecksTheActiveModes()
        {
            _store.Add(OnButton(new OperatingModeCondition { Mode = "vacation", Active = false }));
            await _engine.StartAsync();

            await _environment.PublishAsync(_button, true);
            await Eventually(() => _store.Runs.Count == 1, "first run stored");
            Assert.Equal(AutomationRunState.Done, _store.Runs.Last().State);

            _environment.OperatingModes["vacation"] = true;
            await _environment.PublishAsync(_button, false);
            await _environment.PublishAsync(_button, true);
            await Eventually(() => _store.Runs.Count == 2, "second run stored");
            Assert.Equal(AutomationRunState.ConditionsNotMet, _store.Runs.Last().State);
        }

        [Fact]
        public async Task Condition_UnknownMode_IsNotMet()
        {
            _store.Add(OnButton(new OperatingModeCondition { Mode = "gone" }));
            await _engine.StartAsync();

            await _environment.PublishAsync(_button, true);
            await Eventually(() => _store.Runs.Count == 1, "run stored");
            Assert.Equal(AutomationRunState.ConditionsNotMet, _store.Runs.Single().State);
        }

        [Fact]
        public async Task Validation()
        {
            var problems = await _engine.ValidateAsync(OnButton(new OperatingModeCondition { Mode = "gone" }), null);
            Assert.Contains(problems, a => a.Code == "operatingModeNotFound" && a.Severity == ValidationSeverity.Error);

            problems = await _engine.ValidateAsync(OnButton(new OperatingModeCondition { Mode = "" }), null);
            Assert.Contains(problems, a => a.Code == "valueMissing");

            problems = await _engine.ValidateAsync(OnButton(new OperatingModeCondition { Mode = "vacation" }), null);
            Assert.DoesNotContain(problems, a => a.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void Definition_RoundTrip()
        {
            var json = AutomationJson.Serialize(OnButton(new OperatingModeCondition { Mode = "vacation", Active = false }));
            Assert.Contains("\"type\":\"operatingMode\"", json);

            var condition = Assert.IsType<OperatingModeCondition>(AutomationJson.Parse(json).Conditions.Conditions.Single());
            Assert.Equal("vacation", condition.Mode);
            Assert.False(condition.Active);
        }
    }
}
