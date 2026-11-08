using Automatica.Core.Automation.Definition;
using Automatica.Core.Control.Base;

namespace Automatica.Core.Automation.Engine
{
    internal sealed class EvalContext
    {
        public EvalContext(DateTimeOffset now, string triggerId)
        {
            Now = now;
            TriggerId = triggerId;
        }

        /// <summary>Local time</summary>
        public DateTimeOffset Now { get; }
        public string TriggerId { get; }
    }

    /// <summary>
    /// Evaluates conditions against the current values. With a step sink every leaf condition (and every nested group)
    /// adds a step with its result and the value it compared.
    /// </summary>
    internal sealed class ConditionEvaluator
    {
        private readonly IAutomationEnvironment _environment;

        public ConditionEvaluator(IAutomationEnvironment environment)
        {
            _environment = environment;
        }

        public EvalContext CreateContext(string triggerId)
        {
            return new EvalContext(LocalTime.Now(_environment.Time), triggerId);
        }

        public bool Evaluate(ConditionDefinition condition, string path, EvalContext context, Action<RunStep> steps)
        {
            if (condition == null)
            {
                return true;
            }

            object value = null;
            bool passed;
            string message = null;

            switch (condition)
            {
                case ConditionGroup group:
                    passed = EvaluateGroup(group, path, context, steps);
                    if (path == "conditions")
                    {
                        // the root group itself is no step, only its children
                        return passed;
                    }
                    break;
                case DatapointCondition datapoint:
                    value = _environment.GetValue(datapoint.Node);
                    passed = AutomationValues.Matches(value, datapoint.Operator, AutomationValues.FromJson(datapoint.Value), AutomationValues.FromJson(datapoint.Value2));
                    if (value == null)
                    {
                        message = "Value unknown";
                    }
                    break;
                case TimeCondition time:
                    passed = EvaluateTime(time, context.Now);
                    value = context.Now.ToString("HH:mm");
                    break;
                case SunCondition sun:
                    passed = EvaluateSun(sun, context.Now, out var range);
                    value = range;
                    break;
                case ControlCondition control:
                    var instance = _environment.GetControl(control.Control);
                    if (instance is ISwitch switchControl)
                    {
                        value = switchControl.State == SwitchState.On;
                        passed = switchControl.State != SwitchState.Unknown && (switchControl.State == SwitchState.On) == control.On;
                    }
                    else
                    {
                        passed = false;
                        message = instance == null ? "Control not found" : "Control has no on/off state";
                    }
                    break;
                case ShadingCondition shading:
                    var isShading = _environment.IsShading(shading.Control);
                    value = isShading;
                    passed = isShading.HasValue && isShading.Value == shading.Shading;
                    if (!isShading.HasValue)
                    {
                        message = "Blind has no automatic shading";
                    }
                    break;
                case OperatingModeCondition operatingMode:
                    var modeActive = _environment.IsOperatingModeActive(operatingMode.Mode);
                    value = modeActive;
                    passed = modeActive.HasValue && modeActive.Value == operatingMode.Active;
                    if (!modeActive.HasValue)
                    {
                        message = "Operating mode not found";
                    }
                    break;
                case TriggeredByCondition triggeredBy:
                    value = context.TriggerId;
                    passed = !string.IsNullOrEmpty(triggeredBy.TriggerId) && string.Equals(triggeredBy.TriggerId, context.TriggerId, StringComparison.Ordinal);
                    break;
                default:
                    passed = false;
                    message = $"Unknown condition {condition.GetType().Name}";
                    break;
            }

            steps?.Invoke(new RunStep
            {
                At = _environment.Time.GetUtcNow().UtcDateTime,
                Kind = RunStepKind.Condition,
                Path = path,
                Passed = passed,
                Value = AutomationValues.ToPlain(value),
                Message = message
            });

            return passed;
        }

        private bool EvaluateGroup(ConditionGroup group, string path, EvalContext context, Action<RunStep> steps)
        {
            var conditions = group.Conditions ?? new List<ConditionDefinition>();
            var results = new List<bool>(conditions.Count);

            // all children are evaluated (no short circuit), the run history shows every value
            for (var i = 0; i < conditions.Count; i++)
            {
                results.Add(Evaluate(conditions[i], $"{path}/{i}", context, steps));
            }

            return group.Operator switch
            {
                GroupOperator.All => results.All(a => a),
                GroupOperator.Any => results.Any(a => a),
                GroupOperator.Not => !results.Any(a => a),
                _ => false
            };
        }

        internal static bool InRange(TimeSpan now, TimeSpan? after, TimeSpan? before)
        {
            if (after.HasValue && before.HasValue)
            {
                if (after.Value <= before.Value)
                {
                    return now >= after.Value && now < before.Value;
                }
                // wraps midnight, e.g. 22:00 - 06:00
                return now >= after.Value || now < before.Value;
            }

            if (after.HasValue)
            {
                return now >= after.Value;
            }

            if (before.HasValue)
            {
                return now < before.Value;
            }

            return true;
        }

        private static bool EvaluateTime(TimeCondition condition, DateTimeOffset now)
        {
            if (!LocalTime.DayAllowed(condition.Days, now.DayOfWeek))
            {
                return false;
            }

            TimeSpan? after = AutomationValues.TryParseTime(condition.After, out var a) ? a : null;
            TimeSpan? before = AutomationValues.TryParseTime(condition.Before, out var b) ? b : null;

            return InRange(now.TimeOfDay, after, before);
        }

        private bool EvaluateSun(SunCondition condition, DateTimeOffset now, out string range)
        {
            var today = DateOnly.FromDateTime(now.DateTime);

            DateTimeOffset? after = null;
            DateTimeOffset? before = null;
            if (condition.After.HasValue)
            {
                after = _environment.GetSunTime(today, condition.After.Value)?.AddMinutes(condition.AfterOffsetMinutes);
            }
            if (condition.Before.HasValue)
            {
                before = _environment.GetSunTime(today, condition.Before.Value)?.AddMinutes(condition.BeforeOffsetMinutes);
            }

            range = $"{after?.ToString("HH:mm") ?? "–"} → {before?.ToString("HH:mm") ?? "–"}";

            if ((condition.After.HasValue && after == null) || (condition.Before.HasValue && before == null))
            {
                // no sunrise/sunset today (polar day/night)
                return false;
            }

            return InRange(now.TimeOfDay,
                after.HasValue ? after.Value.ToOffset(now.Offset).TimeOfDay : null,
                before.HasValue ? before.Value.ToOffset(now.Offset).TimeOfDay : null);
        }

        /// <summary>
        /// Datapoints a condition reads, to re-evaluate it when they change
        /// </summary>
        public static void CollectNodes(ConditionDefinition condition, ISet<Guid> nodes, ISet<Guid> controls, ref bool timeBased)
        {
            switch (condition)
            {
                case ConditionGroup group:
                    foreach (var child in group.Conditions ?? new List<ConditionDefinition>())
                    {
                        CollectNodes(child, nodes, controls, ref timeBased);
                    }
                    break;
                case DatapointCondition datapoint:
                    nodes.Add(datapoint.Node);
                    break;
                case ControlCondition control:
                    controls.Add(control.Control);
                    break;
                case TimeCondition:
                case SunCondition:
                case ShadingCondition:
                case OperatingModeCondition:
                    timeBased = true;
                    break;
            }
        }
    }
}
