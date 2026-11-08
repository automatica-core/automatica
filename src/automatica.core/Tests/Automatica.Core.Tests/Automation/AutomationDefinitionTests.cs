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
    public class AutomationDefinitionTests
    {
        [Fact]
        public void Parse_ReadsAllBlockTypes_TypeDiscriminatorAnywhere()
        {
            var node = Guid.NewGuid();
            var json = $$"""
            {
              "schema": 1,
              "triggers": [
                { "id": "motion", "node": "{{node}}", "type": "datapoint", "change": "to", "value": true, "forSeconds": 5 },
                { "type": "time", "at": "06:30", "days": ["monday", "friday"] },
                { "type": "sun", "event": "sunset", "offsetMinutes": -15 }
              ],
              "conditions": { "type": "group", "operator": "any", "conditions": [
                { "type": "time", "after": "22:00", "before": "06:00" },
                { "type": "group", "operator": "not", "conditions": [ { "type": "trigger", "triggerId": "motion" } ] }
              ]},
              "actions": [
                { "type": "if", "condition": { "type": "sun", "after": "sunset", "before": "sunrise" },
                  "then": [ { "type": "control", "control": "{{node}}", "command": "dim", "value": 30 } ],
                  "else": [ { "type": "stop" } ] },
                { "type": "waitUntil", "condition": { "type": "datapoint", "node": "{{node}}", "operator": "eq", "value": false }, "forSeconds": 180 },
                { "type": "notify", "subject": "x", "severity": "warning", "email": ["a@b.c"] }
              ]
            }
            """;

            var definition = AutomationJson.Parse(json);

            var datapoint = Assert.IsType<DatapointTrigger>(definition.Triggers[0]);
            Assert.Equal(DatapointChange.To, datapoint.Change);
            Assert.Equal(5, datapoint.ForSeconds);
            Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Friday }, Assert.IsType<TimeTrigger>(definition.Triggers[1]).Days);
            Assert.Equal(SunEvent.Sunset, Assert.IsType<SunTrigger>(definition.Triggers[2]).Event);

            Assert.Equal(GroupOperator.Any, definition.Conditions.Operator);
            Assert.IsType<TriggeredByCondition>(Assert.IsType<ConditionGroup>(definition.Conditions.Conditions[1]).Conditions[0]);

            var ifAction = Assert.IsType<IfAction>(definition.Actions[0]);
            Assert.Equal(ControlCommand.Dim, Assert.IsType<ControlAction>(ifAction.Then[0]).Command);
            Assert.IsType<StopAction>(ifAction.Else[0]);
            Assert.Equal(NotifySeverity.Warning, Assert.IsType<NotifyAction>(definition.Actions[2]).Severity);

            // round trip keeps everything
            var again = AutomationJson.Parse(AutomationJson.Serialize(definition));
            Assert.Equal(AutomationJson.Serialize(definition), AutomationJson.Serialize(again));
            Assert.Contains("\"type\":\"datapoint\"", AutomationJson.Serialize(definition));
        }

        [Fact]
        public void Parse_UnknownType_Throws()
        {
            Assert.Throws<AutomationDefinitionException>(() => AutomationJson.Parse("""{ "triggers": [ { "type": "teleport" } ] }"""));
        }

        [Theory]
        [InlineData("23:00", "22:00", "06:00", true)]
        [InlineData("05:59", "22:00", "06:00", true)]
        [InlineData("06:00", "22:00", "06:00", false)]
        [InlineData("12:00", "08:00", "17:00", true)]
        [InlineData("17:00", "08:00", "17:00", false)]
        [InlineData("07:00", "08:00", null, false)]
        [InlineData("07:00", null, "08:00", true)]
        public void TimeRange_WrapsMidnight(string now, string after, string before, bool expected)
        {
            TimeSpan? a = after == null ? null : TimeSpan.Parse(after);
            TimeSpan? b = before == null ? null : TimeSpan.Parse(before);
            Assert.Equal(expected, ConditionEvaluator.InRange(TimeSpan.Parse(now), a, b));
        }

        [Theory]
        [InlineData(true, 1.0, true)]
        [InlineData(false, 0, true)]
        [InlineData("on", true, true)]
        [InlineData("21.5", 21.5, true)]
        [InlineData("Open", "open", true)]
        [InlineData(1, 2, false)]
        public void Values_AreEqual(object a, object b, bool expected)
        {
            Assert.Equal(expected, AutomationValues.AreEqual(a, b));
        }

        [Fact]
        public void LocalTime_NextTime_HandlesDaylightSaving()
        {
            var vienna = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Vienna");
            var time = CreateTime(new DateTimeOffset(2026, 3, 28, 12, 0, 0, TimeSpan.Zero));
            time.SetLocalTimeZone(vienna);

            // 02:30 does not exist on 2026-03-29 (02:00 -> 03:00)
            var next = LocalTime.NextTime(time, TimeSpan.FromHours(2.5), Array.Empty<DayOfWeek>(), time.GetUtcNow());
            Assert.NotNull(next);
            Assert.Equal(new DateTimeOffset(2026, 3, 29, 3, 0, 0, TimeSpan.FromHours(2)), next.Value);

            var friday = LocalTime.NextTime(time, TimeSpan.FromHours(7), new[] { DayOfWeek.Friday }, time.GetUtcNow());
            Assert.Equal(DayOfWeek.Friday, friday!.Value.DayOfWeek);
            Assert.Equal(new DateTimeOffset(2026, 4, 3, 7, 0, 0, TimeSpan.FromHours(2)), friday.Value);
        }

        [Fact]
        public async Task Validate_ReportsProblems()
        {
            var environment = new FakeAutomationEnvironment(CreateTime()) { HasLocation = false };
            var store = new InMemoryAutomationStore();
            var engine = CreateEngine(environment, store);
            var readOnly = environment.AddNode("Sensor", writeable: false);

            var definition = new AutomationDefinition
            {
                Triggers =
                {
                    new DatapointTrigger { Node = Guid.NewGuid(), Change = DatapointChange.To },
                    new TimeTrigger { At = "25:00" },
                    new SunTrigger { Event = SunEvent.Sunset }
                },
                Actions =
                {
                    new SetValueAction { Node = readOnly, Value = Json(true) },
                    new RunAutomationAction { Automation = Guid.NewGuid() },
                    new GroupControlAction(),
                    new MailAction { Subject = "x" },
                    new MailAction { To = { "ok@example.com; not-an-address" } }
                }
            };

            var problems = await engine.ValidateAsync(definition, null);

            Assert.Contains(problems, a => a.Path == "actions/3" && a.Code == "recipientMissing");
            Assert.Contains(problems, a => a.Path == "actions/4" && a.Code == "invalidMailAddress" && a.Detail == "not-an-address");
            Assert.Contains(problems, a => a.Path == "actions/4" && a.Code == "valueMissing");

            Assert.Contains(problems, a => a.Path == "triggers/0" && a.Code == "nodeNotFound");
            Assert.Contains(problems, a => a.Path == "triggers/0" && a.Code == "valueMissing");
            Assert.Contains(problems, a => a.Path == "triggers/1" && a.Code == "invalidTime");
            Assert.Contains(problems, a => a.Path == "triggers/2" && a.Code == "locationMissing" && a.Severity == ValidationSeverity.Warning);
            Assert.Contains(problems, a => a.Path == "actions/0" && a.Code == "nodeNotWritable");
            Assert.Contains(problems, a => a.Path == "actions/1" && a.Code == "automationNotFound");
            Assert.Contains(problems, a => a.Path == "actions/2" && a.Code == "groupTargetMissing");
        }

        [Fact]
        public void References_CollectsNestedObjects()
        {
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var c = Guid.NewGuid();
            var definition = new AutomationDefinition
            {
                Triggers = { new DatapointTrigger { Node = a } },
                Actions =
                {
                    new IfAction
                    {
                        Condition = new DatapointCondition { Node = b },
                        Then = { new ControlAction { Control = c } }
                    }
                }
            };

            var references = AutomationReferences.Collect(definition);

            Assert.Contains(references, r => r.Id == a && r.Role == AutomationReferenceRole.Trigger);
            Assert.Contains(references, r => r.Id == b && r.Role == AutomationReferenceRole.Action);
            Assert.Contains(references, r => r.Id == c && r.Type == AutomationReferenceType.Control);
            Assert.Equal(3, references.Count);
        }
    }
}
