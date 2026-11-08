using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Engine;
using Automatica.Core.EF.Models.Automations;
using Xunit;
using static Automatica.Core.Tests.Automation.AutomationTestHelper;

namespace Automatica.Core.Tests.Automation
{
    public class AutomationEngineTests
    {
        private readonly FakeAutomationEnvironment _environment;
        private readonly InMemoryAutomationStore _store = new();
        private readonly AutomationEngine _engine;

        private readonly Guid _motion;
        private readonly Guid _brightness;
        private readonly Guid _light;

        public AutomationEngineTests()
        {
            _environment = new FakeAutomationEnvironment(CreateTime());
            _engine = CreateEngine(_environment, _store);

            _motion = _environment.AddNode("Motion", value: false);
            _brightness = _environment.AddNode("Brightness", AutomationValueType.Number, writeable: false, value: 12.0);
            _light = _environment.AddNode("Light");
        }

        private AutomationDefinition MotionLight(params ActionDefinition[] actions)
        {
            return new AutomationDefinition
            {
                Triggers = { new DatapointTrigger { Id = "motion", Node = _motion, Change = DatapointChange.To, Value = Json(true) } },
                Conditions = new ConditionGroup
                {
                    Conditions = { new DatapointCondition { Node = _brightness, Operator = CompareOperator.Lt, Value = Json(40) } }
                },
                Actions = actions.Length > 0 ? actions.ToList() : new List<ActionDefinition> { new SetValueAction { Node = _light, Value = Json(true) } }
            };
        }

        [Fact]
        public async Task DatapointTrigger_FiresOnEdge_AndWritesValue()
        {
            var automation = _store.Add(MotionLight());
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _store.Runs.Count == 1, "first run stored");

            Assert.Single(_environment.Writes);
            Assert.Equal((_light, (object)true), _environment.Writes.Single());

            var run = _store.Runs.Single();
            Assert.Equal(AutomationRunState.Done, run.State);
            Assert.Equal("motion", run.TriggerId);
            Assert.Equal(automation.ObjId, run.This2Automation);

            // same value again is no edge
            await _environment.PublishAsync(_motion, true);
            await Settle();
            Assert.Single(_store.Runs);

            // off and on again is the next edge
            await _environment.PublishAsync(_motion, false);
            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _store.Runs.Count == 2, "second run stored");
        }

        [Fact]
        public async Task DatapointTrigger_UnknownPreviousValue_DoesNotFire()
        {
            var unknown = _environment.AddNode("Unknown");
            var definition = MotionLight();
            ((DatapointTrigger)definition.Triggers[0]).Node = unknown;
            _store.Add(definition);
            await _engine.StartAsync();

            await _environment.PublishAsync(unknown, true);
            await Settle();
            Assert.Empty(_store.Runs);
        }

        [Fact]
        public async Task Conditions_NotMet_StopsRunWithoutActions()
        {
            _store.Add(MotionLight());
            await _engine.StartAsync();
            await _environment.PublishAsync(_brightness, 80.0);

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _store.Runs.Count == 1, "run stored");

            Assert.Empty(_environment.Writes);
            Assert.Equal(AutomationRunState.ConditionsNotMet, _store.Runs.Single().State);
        }

        [Fact]
        public async Task DatapointTrigger_ForSeconds_FiresAfterDuration_AndCancelsWhenLeft()
        {
            var definition = MotionLight();
            ((DatapointTrigger)definition.Triggers[0]).ForSeconds = 60;
            _store.Add(definition);
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(30));
            await _environment.PublishAsync(_motion, false);
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(60));
            await Settle();
            Assert.Empty(_store.Runs);

            await _environment.PublishAsync(_motion, true);
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(59));
            await Settle();
            Assert.Empty(_store.Runs);

            _environment.FakeTime.Advance(TimeSpan.FromSeconds(1));
            await Eventually(() => _store.Runs.Count == 1, "run after 60 s");
        }

        [Fact]
        public async Task ModeRestart_NewTrigger_StopsWaitingRun()
        {
            var automation = _store.Add(MotionLight(
                new SetValueAction { Node = _light, Value = Json(true) },
                new DelayAction { Seconds = 180 },
                new SetValueAction { Node = _light, Value = Json(false) }), AutomationMode.Restart);
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _engine.GetActiveRunCount(automation.ObjId) == 1 && _environment.Writes.Count == 1, "first run waits");

            await _environment.PublishAsync(_motion, false);
            await _environment.PublishAsync(_motion, true);

            await Eventually(() => _store.Runs.Count == 1, "first run stopped");
            Assert.Equal(AutomationRunState.Stopped, _store.Runs.Single().State);

            await Eventually(() => _environment.Writes.Count == 2, "second run switched on");
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(180));
            await Eventually(() => _store.Runs.Count == 2, "second run done");

            Assert.Equal(AutomationRunState.Done, _store.Runs.Last().State);
            Assert.Equal(false, _environment.Writes.Last().Value);
        }

        [Fact]
        public async Task ModeSingle_IgnoresTriggersWhileRunning()
        {
            var automation = _store.Add(MotionLight(new DelayAction { Seconds = 60 }), AutomationMode.Single);
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _engine.GetActiveRunCount(automation.ObjId) == 1, "run active");
            await _environment.PublishAsync(_motion, false);
            await _environment.PublishAsync(_motion, true);
            await Settle();

            Assert.Equal(1, _engine.GetActiveRunCount(automation.ObjId));
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(60));
            await Eventually(() => _store.Runs.Count == 1, "single run done");
            await Settle();
            Assert.Single(_store.Runs);
        }

        [Fact]
        public async Task ModeQueued_RunsOneAfterTheOther()
        {
            var automation = _store.Add(MotionLight(new DelayAction { Seconds = 10 }, new SetValueAction { Node = _light, Value = Json(true) }), AutomationMode.Queued);
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _engine.GetActiveRunCount(automation.ObjId) == 1, "run active");
            await _environment.PublishAsync(_motion, false);
            await _environment.PublishAsync(_motion, true);
            await Settle();
            Assert.Equal(1, _engine.GetActiveRunCount(automation.ObjId));

            _environment.FakeTime.Advance(TimeSpan.FromSeconds(10));
            await Eventually(() => _store.Runs.Count == 1, "first done");
            await Eventually(() => _engine.GetActiveRunCount(automation.ObjId) == 1, "queued run started");
            await Settle();
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(10));
            await Eventually(() => _store.Runs.Count == 2, "queued run done");
            Assert.Equal(2, _environment.Writes.Count);
        }

        [Fact]
        public async Task OwnWrite_DoesNotTriggerTheSameAutomationAgain()
        {
            _environment.ReadBackWrites = true;
            var light = _environment.AddNode("Toggle", value: false);
            _store.Add(new AutomationDefinition
            {
                Triggers = { new DatapointTrigger { Id = "any", Node = light, Change = DatapointChange.Any } },
                Actions = { new SetValueAction { Node = light, Mode = SetValueMode.Toggle } }
            });
            await _engine.StartAsync();

            await _environment.PublishAsync(light, true);
            await Eventually(() => _store.Runs.Count == 1, "first run");
            await Settle();

            Assert.Single(_store.Runs);
            Assert.Equal(false, _environment.Writes.Single().Value);
        }

        [Fact]
        public async Task RateLimit_SwitchesTheAutomationOff()
        {
            var automation = _store.Add(MotionLight());
            await _engine.StartAsync();

            for (var i = 0; i <= AutomationRuntime.RateLimitRuns + 1; i++)
            {
                await _environment.PublishAsync(_motion, false);
                await _environment.PublishAsync(_motion, true);
            }

            await Eventually(() => !_store.Automations[automation.ObjId].IsEnabled, "switched off");
            await Eventually(() => _environment.Notifications.Count == 1, "notification");
            await Eventually(() => _environment.SubscriberCount(_motion) == 0, "trigger stopped");
        }

        [Fact]
        public async Task TimeTrigger_FiresAtTheTime_EveryDay()
        {
            _store.Add(new AutomationDefinition
            {
                Triggers = { new TimeTrigger { Id = "evening", At = "21:30" } },
                Actions = { new SetValueAction { Node = _light, Value = Json(true) } }
            });
            await _engine.StartAsync();

            _environment.FakeTime.Advance(TimeSpan.FromMinutes(89));
            await Settle();
            Assert.Empty(_store.Runs);

            _environment.FakeTime.Advance(TimeSpan.FromMinutes(1));
            await Eventually(() => _store.Runs.Count == 1, "21:30 run");

            _environment.FakeTime.Advance(TimeSpan.FromDays(1));
            await Eventually(() => _store.Runs.Count == 2, "next day run");
        }

        [Fact]
        public async Task SunTrigger_UsesOffset()
        {
            // starts 2026-11-09 20:00 UTC, sunrise of the fake is 07:00
            _store.Add(new AutomationDefinition
            {
                Triggers = { new SunTrigger { Id = "sunrise", Event = SunEvent.Sunrise, OffsetMinutes = -15 } },
                Actions = { new SetValueAction { Node = _light, Value = Json(false) } }
            });
            await _engine.StartAsync();

            _environment.FakeTime.Advance(TimeSpan.FromHours(10) + TimeSpan.FromMinutes(44));
            await Settle();
            Assert.Empty(_store.Runs);

            _environment.FakeTime.Advance(TimeSpan.FromMinutes(1));
            await Eventually(() => _store.Runs.Count == 1, "06:45 run");
        }

        [Fact]
        public async Task WaitUntil_WithDuration_ContinuesWhenMet()
        {
            var automation = _store.Add(MotionLight(
                new SetValueAction { Node = _light, Value = Json(true) },
                new WaitUntilAction
                {
                    Condition = new DatapointCondition { Node = _motion, Operator = CompareOperator.Eq, Value = Json(false) },
                    ForSeconds = 180,
                    TimeoutSeconds = 1800
                },
                new SetValueAction { Node = _light, Value = Json(false) }), AutomationMode.Restart);
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _environment.Writes.Count == 1, "light on");
            await Settle();

            await _environment.PublishAsync(_motion, false);
            await Settle();
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(179));
            await Settle();
            Assert.Equal(1, _environment.Writes.Count);

            _environment.FakeTime.Advance(TimeSpan.FromSeconds(1));
            await Eventually(() => _environment.Writes.Count == 2, "light off after 3 min");
            await Eventually(() => _store.Runs.Count == 1, "run done");

            var steps = (await _engine.GetRunsAsync(automation.ObjId)).Single().Steps;
            Assert.Contains(steps, a => a.Path == "actions/1" && a.State == RunStepState.Done && a.Passed == true);
        }

        [Fact]
        public async Task WaitUntil_Timeout_StopsWhenNotContinuing()
        {
            _store.Add(MotionLight(
                new WaitUntilAction
                {
                    Condition = new DatapointCondition { Node = _motion, Operator = CompareOperator.Eq, Value = Json(false) },
                    TimeoutSeconds = 60,
                    ContinueOnTimeout = false
                },
                new SetValueAction { Node = _light, Value = Json(false) }));
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Settle();
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(60));
            await Eventually(() => _store.Runs.Count == 1, "run ended");

            Assert.Empty(_environment.Writes);
            Assert.Equal(AutomationRunState.Done, _store.Runs.Single().State);
        }

        [Fact]
        public async Task IfAction_RunsElseBranch_AndRecordsPaths()
        {
            var automation = _store.Add(new AutomationDefinition
            {
                Triggers = { new StartupTrigger { Id = "start" } },
                Actions =
                {
                    new IfAction
                    {
                        Condition = new DatapointCondition { Node = _brightness, Operator = CompareOperator.Gt, Value = Json(100) },
                        Then = { new SetValueAction { Node = _light, Value = Json(false) } },
                        Else = { new SetValueAction { Node = _light, Value = Json(true) } }
                    }
                }
            });
            await _engine.StartAsync();
            await Eventually(() => _store.Runs.Count == 1, "startup run");

            Assert.Equal(true, _environment.Writes.Single().Value);
            var steps = (await _engine.GetRunsAsync(automation.ObjId)).Single().Steps;
            Assert.Contains(steps, a => a.Path == "actions/0/condition" && a.Passed == false);
            Assert.Contains(steps, a => a.Path == "actions/0/else/0" && a.State == RunStepState.Done);
        }

        [Fact]
        public async Task FailingAction_FailsRun_UnlessContinueOnError()
        {
            var readOnly = _brightness;
            var automation = _store.Add(MotionLight(
                new SetValueAction { Node = readOnly, Value = Json(1), ContinueOnError = true },
                new SetValueAction { Node = readOnly, Value = Json(2) },
                new SetValueAction { Node = _light, Value = Json(true) }));
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _store.Runs.Count == 1, "run stored");

            var run = _store.Runs.Single();
            Assert.Equal(AutomationRunState.Failed, run.State);
            Assert.Empty(_environment.Writes);
            Assert.Contains("cannot be written", run.Error);
        }

        [Fact]
        public async Task ControlAction_And_ControlTrigger()
        {
            var lamp = new FakeSwitch { Name = "Lamp" };
            var other = new FakeSwitch { Name = "Other" };
            _environment.Controls[lamp.Id] = lamp;
            _environment.Controls[other.Id] = other;

            _store.Add(new AutomationDefinition
            {
                Triggers = { new ControlTrigger { Id = "lamp", Control = lamp.Id, To = ControlStateChange.On } },
                Actions = { new ControlAction { Control = other.Id, Command = ControlCommand.On } }
            });
            await _engine.StartAsync();

            await lamp.SwitchOnAsync();
            await Eventually(() => _store.Runs.Count == 1, "run");
            Assert.Equal(Control.Base.SwitchState.On, other.State);
        }

        [Fact]
        public async Task SceneTrigger_FiresOnEveryActivationPulse()
        {
            var scene = Guid.NewGuid();
            var activated = Guid.NewGuid();
            _environment.LogicOutputs[(scene, SceneTrigger.ActivatedOutputKey)] = activated;

            _store.Add(new AutomationDefinition
            {
                Triggers = { new SceneTrigger { Id = "scene", Scene = scene } },
                Actions = { new SetValueAction { Node = _light, Value = Json(true) } }
            });
            await _engine.StartAsync();
            Assert.Equal(1, _environment.SubscriberCount(activated));

            // the scene block pulses 1 and back to 0 on every activation
            await _environment.PublishAsync(activated, true);
            await Eventually(() => _store.Runs.Count == 1, "first activation");
            await _environment.PublishAsync(activated, false);
            await _environment.PublishAsync(activated, true);
            await Eventually(() => _store.Runs.Count == 2, "second activation");

            Assert.All(_store.Runs, r => Assert.Equal("scene", r.TriggerId));
        }

        [Fact]
        public async Task SceneTrigger_UnknownScene_IsAValidationError_AndDoesNotStart()
        {
            var definition = new AutomationDefinition
            {
                Triggers = { new SceneTrigger { Id = "scene", Scene = Guid.NewGuid() } },
                Actions = { new SetValueAction { Node = _light, Value = Json(true) } }
            };

            var problems = await _engine.ValidateAsync(definition, null);
            Assert.Contains(problems, p => p.Code == "sceneNotFound" && p.Path == "triggers/0");

            _store.Add(definition);
            await _engine.StartAsync();
            Assert.Empty(_store.Runs);
        }

        [Fact]
        public async Task ManualRun_OfDisabledAutomation_SkipsConditions()
        {
            var automation = _store.Add(MotionLight(), enabled: false);
            await _engine.StartAsync();
            await _environment.PublishAsync(_brightness, 80.0);

            Assert.Equal(0, _environment.SubscriberCount(_motion));

            var runId = await _engine.RunAsync(automation.ObjId, skipConditions: true);
            Assert.NotNull(runId);
            await Eventually(() => _store.Runs.Count == 1, "manual run");
            Assert.Equal(AutomationRunState.Done, _store.Runs.Single().State);
            Assert.Single(_environment.Writes);
        }

        [Fact]
        public async Task Reload_DisabledAutomation_StopsTriggers()
        {
            var automation = _store.Add(MotionLight());
            await _engine.StartAsync();
            Assert.Equal(1, _environment.SubscriberCount(_motion));

            await _engine.SetEnabledAsync(automation.ObjId, false);
            Assert.Equal(0, _environment.SubscriberCount(_motion));

            await _engine.SetEnabledAsync(automation.ObjId, true);
            Assert.Equal(1, _environment.SubscriberCount(_motion));

            await _engine.StopAsync();
            Assert.Equal(0, _environment.SubscriberCount(_motion));
        }

        [Fact]
        public async Task ConditionsTrigger_FiresWhenConditionsBecomeTrue()
        {
            _store.Add(new AutomationDefinition
            {
                Triggers =
                {
                    new ConditionsTrigger
                    {
                        Id = "dark-and-motion",
                        Conditions = new ConditionGroup
                        {
                            Conditions =
                            {
                                new DatapointCondition { Node = _motion, Operator = CompareOperator.Eq, Value = Json(true) },
                                new DatapointCondition { Node = _brightness, Operator = CompareOperator.Lt, Value = Json(40) }
                            }
                        }
                    }
                },
                Actions = { new SetValueAction { Node = _light, Value = Json(true) } }
            });
            await _engine.StartAsync();

            await _environment.PublishAsync(_brightness, 80.0);
            await _environment.PublishAsync(_motion, true);
            await Settle();
            Assert.Empty(_store.Runs);

            await _environment.PublishAsync(_brightness, 20.0);
            await Eventually(() => _store.Runs.Count == 1, "conditions became true");

            await _environment.PublishAsync(_brightness, 10.0);
            await Settle();
            Assert.Single(_store.Runs);
        }

        [Fact]
        public async Task Notify_RendersPlaceholders()
        {
            _store.Add(MotionLight(new NotifyAction { Subject = "{automation}: {trigger}", Body = "Brightness {value:" + _brightness + "}, value {trigger.value}" }), name: "Hallway");
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _environment.Notifications.Count == 1, "notification");

            var (subject, body) = _environment.Notifications.Single();
            Assert.Equal("Hallway: motion", subject);
            Assert.Equal("Brightness 12, value on", body);
        }

        [Fact]
        public async Task Mail_SendsToEveryRecipientWithPlaceholders()
        {
            _store.Add(MotionLight(new MailAction
            {
                To = { "a@example.com; b@example.com", "A@example.com" },
                Subject = "{automation} at {time}",
                Body = "Brightness {value:" + _brightness + "} on {date}"
            }), name: "Hallway");
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _environment.Emails.Count == 1, "mail");

            var (to, subject, body) = _environment.Emails.Single();
            Assert.Equal(new[] { "a@example.com", "b@example.com" }, to);
            var now = _environment.Time.GetLocalNow();
            Assert.Equal($"Hallway at {now.ToString("t", System.Globalization.CultureInfo.CurrentCulture)}", subject);
            Assert.Equal($"Brightness 12 on {now.ToString("d", System.Globalization.CultureInfo.CurrentCulture)}", body);
            Assert.Empty(_environment.Notifications);
            await Eventually(() => _store.Runs.Count == 1 && _store.Runs.Single().State == AutomationRunState.Done, "run done");
        }

        [Fact]
        public async Task Mail_FailsTheRunWhenTheMailIsNotSent()
        {
            _environment.EmailResult = false;
            _store.Add(MotionLight(new MailAction { To = { "a@example.com" }, Subject = "x" }));
            await _engine.StartAsync();

            await _environment.PublishAsync(_motion, true);
            await Eventually(() => _store.Runs.Count == 1 && _store.Runs.Single().State == AutomationRunState.Failed, "run failed");
        }

        [Fact]
        public void Test_EvaluatesConditionsWithValues()
        {
            var result = _engine.Test(MotionLight(), "motion");

            Assert.True(result.Passed);
            var step = Assert.Single(result.Steps);
            Assert.Equal("conditions/0", step.Path);
            Assert.Equal(12.0, step.Value);
        }
    }
}
