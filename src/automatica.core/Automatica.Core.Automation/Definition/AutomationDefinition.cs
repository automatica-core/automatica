using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automatica.Core.Automation.Definition
{
    /// <summary>
    /// The JSON document stored in <c>Automation.Definition</c>. Version 1:
    /// <code>
    /// { "schema": 1, "triggers": [...], "conditions": { "type": "group", "operator": "all", "conditions": [...] }, "actions": [...] }
    /// </code>
    /// Every block has a "type" discriminator. Steps of a run point into this document with a path such as
    /// "triggers/0", "conditions/1/0" (group indices) or "actions/2/then/0".
    /// </summary>
    public sealed class AutomationDefinition
    {
        public const int CurrentSchema = 1;

        public int Schema { get; set; } = CurrentSchema;

        /// <summary>
        /// Any of the triggers starts a run
        /// </summary>
        public List<TriggerDefinition> Triggers { get; set; } = new();

        /// <summary>
        /// Checked when a trigger fired, the run stops when they are not met
        /// </summary>
        public ConditionGroup Conditions { get; set; } = new();

        /// <summary>
        /// Executed one after the other
        /// </summary>
        public List<ActionDefinition> Actions { get; set; } = new();
    }

    public enum SunEvent
    {
        Sunrise,
        Sunset,
        Dawn,
        Dusk
    }

    #region triggers

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(DatapointTrigger), "datapoint")]
    [JsonDerivedType(typeof(TimeTrigger), "time")]
    [JsonDerivedType(typeof(SunTrigger), "sun")]
    [JsonDerivedType(typeof(ConditionsTrigger), "conditions")]
    [JsonDerivedType(typeof(ControlTrigger), "control")]
    [JsonDerivedType(typeof(StartupTrigger), "startup")]
    public abstract class TriggerDefinition
    {
        /// <summary>
        /// Free id of the trigger, used by the "started by trigger" condition and shown in the run history
        /// </summary>
        public string Id { get; set; }
    }

    public enum DatapointChange
    {
        /// <summary>Every change of the value</summary>
        Any,
        /// <summary>The value changes to <see cref="DatapointTrigger.Value"/></summary>
        To,
        /// <summary>The value crosses above <see cref="DatapointTrigger.Value"/></summary>
        Above,
        /// <summary>The value crosses below <see cref="DatapointTrigger.Value"/></summary>
        Below
    }

    /// <summary>
    /// Fires when the value of a datapoint changes into the configured state (edge, not level). With
    /// <see cref="ForSeconds"/> the state has to last that long.
    /// </summary>
    public sealed class DatapointTrigger : TriggerDefinition
    {
        public Guid Node { get; set; }
        public DatapointChange Change { get; set; } = DatapointChange.Any;
        public JsonElement? Value { get; set; }

        /// <summary>
        /// Optional, only fire when the previous value was this one
        /// </summary>
        public JsonElement? From { get; set; }

        public int ForSeconds { get; set; }
    }

    /// <summary>
    /// Fires every day (or on the listed <see cref="Days"/>) at <see cref="At"/> local time ("HH:mm" or "HH:mm:ss").
    /// </summary>
    public sealed class TimeTrigger : TriggerDefinition
    {
        public string At { get; set; }
        public List<DayOfWeek> Days { get; set; } = new();
    }

    /// <summary>
    /// Fires at a sun event of the location in the settings (Latitude/Longitude), shifted by <see cref="OffsetMinutes"/>.
    /// </summary>
    public sealed class SunTrigger : TriggerDefinition
    {
        public SunEvent Event { get; set; }
        public int OffsetMinutes { get; set; }
        public List<DayOfWeek> Days { get; set; } = new();
    }

    /// <summary>
    /// Fires when <see cref="Conditions"/> change from "not met" to "met" (the Loxone automatic rule behaviour).
    /// </summary>
    public sealed class ConditionsTrigger : TriggerDefinition
    {
        public ConditionGroup Conditions { get; set; } = new();
    }

    public enum ControlStateChange
    {
        Any,
        On,
        Off
    }

    /// <summary>
    /// Fires when a switch (or dimmer) control is switched on or off.
    /// </summary>
    public sealed class ControlTrigger : TriggerDefinition
    {
        public Guid Control { get; set; }
        public ControlStateChange To { get; set; } = ControlStateChange.Any;
    }

    /// <summary>
    /// Fires once after the server started.
    /// </summary>
    public sealed class StartupTrigger : TriggerDefinition
    {
    }

    #endregion

    #region conditions

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(ConditionGroup), "group")]
    [JsonDerivedType(typeof(DatapointCondition), "datapoint")]
    [JsonDerivedType(typeof(TimeCondition), "time")]
    [JsonDerivedType(typeof(SunCondition), "sun")]
    [JsonDerivedType(typeof(ControlCondition), "control")]
    [JsonDerivedType(typeof(TriggeredByCondition), "trigger")]
    public abstract class ConditionDefinition
    {
    }

    public enum GroupOperator
    {
        /// <summary>All conditions are met (an empty group is met)</summary>
        All,
        /// <summary>At least one condition is met (an empty group is not met)</summary>
        Any,
        /// <summary>None of the conditions is met</summary>
        Not
    }

    public sealed class ConditionGroup : ConditionDefinition
    {
        public GroupOperator Operator { get; set; } = GroupOperator.All;
        public List<ConditionDefinition> Conditions { get; set; } = new();
    }

    public enum CompareOperator
    {
        Eq,
        Ne,
        Lt,
        Le,
        Gt,
        Ge,
        Between
    }

    /// <summary>
    /// Compares the current value of a datapoint. <see cref="Between"/> uses <see cref="Value"/> and <see cref="Value2"/> (inclusive).
    /// </summary>
    public sealed class DatapointCondition : ConditionDefinition
    {
        public Guid Node { get; set; }
        public CompareOperator Operator { get; set; } = CompareOperator.Eq;
        public JsonElement? Value { get; set; }
        public JsonElement? Value2 { get; set; }
    }

    /// <summary>
    /// Local time between <see cref="After"/> and <see cref="Before"/> (wraps midnight when After &gt; Before) and/or one of
    /// the <see cref="Days"/>. Empty parts are not checked.
    /// </summary>
    public sealed class TimeCondition : ConditionDefinition
    {
        public string After { get; set; }
        public string Before { get; set; }
        public List<DayOfWeek> Days { get; set; } = new();
    }

    /// <summary>
    /// After a sun event and/or before a sun event. "After sunset, before sunrise" wraps midnight and means "it is dark".
    /// </summary>
    public sealed class SunCondition : ConditionDefinition
    {
        public SunEvent? After { get; set; }
        public int AfterOffsetMinutes { get; set; }
        public SunEvent? Before { get; set; }
        public int BeforeOffsetMinutes { get; set; }
    }

    /// <summary>
    /// A switch (or dimmer) control is on (or off)
    /// </summary>
    public sealed class ControlCondition : ConditionDefinition
    {
        public Guid Control { get; set; }
        public bool On { get; set; } = true;
    }

    /// <summary>
    /// The run was started by the trigger with this id
    /// </summary>
    public sealed class TriggeredByCondition : ConditionDefinition
    {
        public string TriggerId { get; set; }
    }

    #endregion

    #region actions

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(SetValueAction), "setValue")]
    [JsonDerivedType(typeof(ControlAction), "control")]
    [JsonDerivedType(typeof(GroupControlAction), "groupControl")]
    [JsonDerivedType(typeof(DelayAction), "delay")]
    [JsonDerivedType(typeof(WaitUntilAction), "waitUntil")]
    [JsonDerivedType(typeof(NotifyAction), "notify")]
    [JsonDerivedType(typeof(IfAction), "if")]
    [JsonDerivedType(typeof(RunAutomationAction), "runAutomation")]
    [JsonDerivedType(typeof(EnableAutomationAction), "enableAutomation")]
    [JsonDerivedType(typeof(StopAction), "stop")]
    public abstract class ActionDefinition
    {
        /// <summary>
        /// A disabled action is skipped
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// When true a failing action is logged in the run and the run continues
        /// </summary>
        public bool ContinueOnError { get; set; }
    }

    public enum SetValueMode
    {
        /// <summary>Writes <see cref="SetValueAction.Value"/></summary>
        Value,
        /// <summary>Writes the inverted current boolean value</summary>
        Toggle,
        /// <summary>Adds <see cref="SetValueAction.Step"/> to the current numeric value</summary>
        Increment,
        /// <summary>Writes the current value of <see cref="SetValueAction.Source"/></summary>
        Copy
    }

    public sealed class SetValueAction : ActionDefinition
    {
        public Guid Node { get; set; }
        public SetValueMode Mode { get; set; } = SetValueMode.Value;
        public JsonElement? Value { get; set; }
        public double Step { get; set; } = 1;
        public Guid? Source { get; set; }
    }

    public enum ControlCommand
    {
        On,
        Off,
        Toggle,
        /// <summary>Dimmer to <see cref="ControlAction.Value"/> percent</summary>
        Dim,
        Up,
        Down,
        Stop,
        /// <summary>Blind to <see cref="ControlAction.Value"/> percent</summary>
        Position
    }

    public sealed class ControlAction : ActionDefinition
    {
        public Guid Control { get; set; }
        public ControlCommand Command { get; set; } = ControlCommand.On;
        public int? Value { get; set; }
    }

    /// <summary>
    /// Sends a command to all controls (switches, dimmers, blinds) whose logic is in the area and/or category,
    /// e.g. "all lights on the ground floor off". Controls that do not support the command are skipped.
    /// </summary>
    public sealed class GroupControlAction : ActionDefinition
    {
        public Guid? Area { get; set; }
        public bool IncludeSubAreas { get; set; } = true;
        public Guid? Category { get; set; }
        public ControlCommand Command { get; set; } = ControlCommand.Off;
        public int? Value { get; set; }
    }

    public sealed class DelayAction : ActionDefinition
    {
        public int Seconds { get; set; }

        /// <summary>
        /// Optional, a random delay between <see cref="Seconds"/> and this value
        /// </summary>
        public int? RandomToSeconds { get; set; }
    }

    /// <summary>
    /// Waits until <see cref="Condition"/> is met (for <see cref="ForSeconds"/>), gives up after <see cref="TimeoutSeconds"/> (0 = never).
    /// </summary>
    public sealed class WaitUntilAction : ActionDefinition
    {
        public ConditionDefinition Condition { get; set; }
        public int ForSeconds { get; set; }
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// On a timeout the run continues (true) or stops (false)
        /// </summary>
        public bool ContinueOnTimeout { get; set; } = true;
    }

    public enum NotifySeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Creates a notification and optionally sends an email. Subject and body can use the placeholders {automation},
    /// {trigger}, {trigger.value} and {value:&lt;datapoint id&gt;}.
    /// </summary>
    public sealed class NotifyAction : ActionDefinition
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public NotifySeverity Severity { get; set; } = NotifySeverity.Info;
        public List<string> Email { get; set; } = new();
    }

    public sealed class IfAction : ActionDefinition
    {
        public ConditionDefinition Condition { get; set; }
        public List<ActionDefinition> Then { get; set; } = new();
        public List<ActionDefinition> Else { get; set; } = new();
    }

    public sealed class RunAutomationAction : ActionDefinition
    {
        public Guid Automation { get; set; }
        public bool SkipConditions { get; set; }
    }

    public sealed class EnableAutomationAction : ActionDefinition
    {
        public Guid Automation { get; set; }
        public bool Enable { get; set; } = true;
    }

    /// <summary>
    /// Ends the run (as done)
    /// </summary>
    public sealed class StopAction : ActionDefinition
    {
        public string Reason { get; set; }
    }

    #endregion
}
