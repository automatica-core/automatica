using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Automatica.Core.Automation.Definition;
using Xunit;

namespace Automatica.Core.Tests.Automation
{
    public class AutomationUserAccessTests
    {
        private static readonly Guid Light = Guid.NewGuid();
        private static readonly Guid Brightness = Guid.NewGuid();

        private static AutomationDefinition Rule(bool userAccess = true) => new()
        {
            UserAccess = userAccess,
            Triggers =
            {
                new TimeTrigger { At = "06:30", Days = { DayOfWeek.Monday }, UserEditable = true },
                new SunTrigger { Event = SunEvent.Sunset },
                new ConditionsTrigger
                {
                    Conditions = new ConditionGroup { Conditions = { new DatapointCondition { Node = Brightness, Operator = CompareOperator.Lt, Value = Json(30), UserEditable = true } } }
                }
            },
            Conditions = new ConditionGroup
            {
                Conditions =
                {
                    new ConditionGroup { Operator = GroupOperator.Any, Conditions = { new TimeCondition { After = "06:00", Before = "22:00", UserEditable = true } } },
                    new ControlCondition { Control = Guid.NewGuid(), UserEditable = true }
                }
            },
            Actions =
            {
                new SetValueAction { Node = Light, Value = Json(80), UserEditable = true },
                new IfAction
                {
                    Condition = new DatapointCondition { Node = Brightness, Operator = CompareOperator.Gt, Value = Json(10), UserEditable = true },
                    Then = { new DelayAction { Seconds = 60, UserEditable = true } }
                },
                new DelayAction { Seconds = 5 }
            }
        };

        private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);

        private static UserBlockEdit Edit(string path, string json) => new() { Path = path, Values = JsonNode.Parse(json)!.AsObject() };

        [Fact]
        public void Collect_FindsMarkedBlocksOfEditableTypes_WithTheirPaths()
        {
            var blocks = AutomationUserAccess.Collect(Rule());

            Assert.Equal(new[] { "triggers/0", "triggers/2/conditions/0", "conditions/0/0", "actions/0", "actions/1/condition", "actions/1/then/0" },
                blocks.Select(a => a.Path));
            var time = blocks[0];
            Assert.Equal("trigger", time.Kind);
            Assert.Equal("time", time.Type);
            Assert.Equal("06:30", time.Values["at"]!.GetValue<string>());
            Assert.Equal(new[] { "at", "days" }, time.Fields);

            // the control condition is marked, but its type cannot be released
            Assert.DoesNotContain(blocks, a => a.Type == "control");
        }

        [Fact]
        public void Collect_NotReleased_IsEmpty()
        {
            Assert.Empty(AutomationUserAccess.Collect(Rule(userAccess: false)));
        }

        [Fact]
        public void Apply_ChangesOnlyTheReleasedFields()
        {
            var changed = AutomationUserAccess.Apply(Rule(), new[]
            {
                Edit("triggers/0", """{ "at": "07:15", "days": ["saturday", "sunday"] }"""),
                Edit("conditions/0/0", """{ "after": "08:00" }"""),
                Edit("actions/0", """{ "value": 40 }"""),
                Edit("actions/1/then/0", """{ "seconds": 120 }"""),
                Edit("triggers/2/conditions/0", """{ "value": 25 }""")
            }, out var errors);

            Assert.Empty(errors);
            var time = Assert.IsType<TimeTrigger>(changed.Triggers[0]);
            Assert.Equal("07:15", time.At);
            Assert.Equal(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, time.Days);
            Assert.True(time.UserEditable);
            Assert.Equal("08:00", ((TimeCondition)((ConditionGroup)changed.Conditions.Conditions[0]).Conditions[0]).After);
            var setValue = Assert.IsType<SetValueAction>(changed.Actions[0]);
            Assert.Equal(40, setValue.Value!.Value.GetInt32());
            Assert.Equal(Light, setValue.Node);
            Assert.Equal(120, ((DelayAction)((IfAction)changed.Actions[1]).Then[0]).Seconds);
            Assert.Equal(25, ((DatapointCondition)((ConditionsTrigger)changed.Triggers[2]).Conditions.Conditions[0]).Value!.Value.GetInt32());
            Assert.True(changed.UserAccess);
            // untouched blocks stay as they were
            Assert.Equal(5, ((DelayAction)changed.Actions[2]).Seconds);
        }

        [Theory]
        // the node of a released block
        [InlineData("actions/0", """{ "node": "00000000-0000-0000-0000-000000000001" }""")]
        // the operator of a released condition
        [InlineData("actions/1/condition", """{ "operator": "eq" }""")]
        // the marks themselves
        [InlineData("triggers/0", """{ "userEditable": false }""")]
        // a block that is not released
        [InlineData("triggers/1", """{ "offsetMinutes": 10 }""")]
        [InlineData("actions/2", """{ "seconds": 1 }""")]
        // a block of a type that cannot be released
        [InlineData("conditions/1", """{ "on": false }""")]
        // paths that do not exist
        [InlineData("actions/9", """{ "seconds": 1 }""")]
        [InlineData("../triggers/0", """{ "at": "01:00" }""")]
        public void Apply_RefusesEverythingElse(string path, string values)
        {
            var current = Rule();
            var before = AutomationJson.Serialize(current);

            var changed = AutomationUserAccess.Apply(current, new[] { Edit(path, values) }, out var errors);

            Assert.Null(changed);
            Assert.NotEmpty(errors);
            Assert.Equal(before, AutomationJson.Serialize(current));
        }

        [Fact]
        public void Apply_InvalidValue_IsRefused()
        {
            var changed = AutomationUserAccess.Apply(Rule(), new[] { Edit("triggers/0", """{ "days": ["funday"] }""") }, out var errors);

            Assert.Null(changed);
            Assert.Single(errors);
        }

        [Fact]
        public void Apply_NotReleased_RefusesAll()
        {
            var changed = AutomationUserAccess.Apply(Rule(userAccess: false), new[] { Edit("triggers/0", """{ "at": "07:00" }""") }, out var errors);

            Assert.Null(changed);
            Assert.Single(errors);
        }

        [Fact]
        public void Flags_AreOnlyWrittenWhenSet()
        {
            var json = AutomationJson.Serialize(new AutomationDefinition { Actions = { new DelayAction { Seconds = 1 } } });

            Assert.DoesNotContain("userEditable", json);
            Assert.DoesNotContain("userAccess", json);

            var roundTrip = AutomationJson.Parse(AutomationJson.Serialize(Rule()));
            Assert.True(roundTrip.UserAccess);
            Assert.True(roundTrip.Triggers[0].UserEditable);
            Assert.False(roundTrip.Triggers[1].UserEditable);
        }
    }
}
