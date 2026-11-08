using Automatica.Core.Automation.Definition;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models.Automations;

namespace Automatica.Core.Automation.Engine
{
    /// <summary>
    /// Checks a definition against the current configuration. Codes are listed in the README.
    /// </summary>
    internal sealed class AutomationValidator
    {
        private readonly IAutomationEnvironment _environment;
        private readonly IReadOnlyCollection<Guid> _automations;
        private readonly Guid? _self;
        private readonly List<ValidationProblem> _problems = new();

        public AutomationValidator(IAutomationEnvironment environment, IReadOnlyCollection<Guid> automations, Guid? self)
        {
            _environment = environment;
            _automations = automations;
            _self = self;
        }

        public IReadOnlyList<ValidationProblem> Validate(AutomationDefinition definition)
        {
            _problems.Clear();

            if (definition.Triggers.Count == 0)
            {
                Warning("", "noTriggers");
            }

            if (definition.Actions.Count == 0)
            {
                Warning("", "noActions");
            }

            for (var i = 0; i < definition.Triggers.Count; i++)
            {
                ValidateTrigger(definition.Triggers[i], $"triggers/{i}");
            }

            ValidateCondition(definition.Conditions, "conditions");
            ValidateActions(definition.Actions, "actions");

            return _problems.ToList();
        }

        private void ValidateMail(MailAction mail, string path)
        {
            var recipients = (mail.To ?? new List<string>()).SelectMany(a => (a ?? "").Split(';', ',')).Select(a => a.Trim()).Where(a => a.Length > 0).ToList();
            if (recipients.Count == 0)
            {
                Error(path, "recipientMissing");
            }

            foreach (var address in recipients.Where(a => !IsMailAddress(a)))
            {
                Error(path, "invalidMailAddress", address);
            }

            if (string.IsNullOrWhiteSpace(mail.Subject) && string.IsNullOrWhiteSpace(mail.Body))
            {
                Error(path, "valueMissing");
            }
        }

        internal static bool IsMailAddress(string value)
        {
            var at = value.IndexOf('@');
            return at > 0 && at < value.Length - 1 && value.IndexOf('@', at + 1) < 0 && !value.Any(char.IsWhiteSpace) && value[(at + 1)..].Contains('.');
        }

        private void Error(string path, string code, string detail = null) => _problems.Add(new ValidationProblem(path, code, ValidationSeverity.Error, detail));
        private void Warning(string path, string code, string detail = null) => _problems.Add(new ValidationProblem(path, code, ValidationSeverity.Warning, detail));

        private void ValidateTrigger(TriggerDefinition trigger, string path)
        {
            switch (trigger)
            {
                case DatapointTrigger datapoint:
                    ReadableNode(datapoint.Node, path);
                    if (datapoint.Change != DatapointChange.Any && AutomationValues.FromJson(datapoint.Value) == null)
                    {
                        Error(path, "valueMissing");
                    }
                    if (datapoint.ForSeconds < 0)
                    {
                        Error(path, "negativeDuration");
                    }
                    break;
                case TimeTrigger time:
                    if (!AutomationValues.TryParseTime(time.At, out _))
                    {
                        Error(path, "invalidTime", time.At);
                    }
                    break;
                case SunTrigger:
                    Location(path);
                    break;
                case ConditionsTrigger conditions:
                    ValidateCondition(conditions.Conditions, $"{path}/conditions");
                    break;
                case ControlTrigger control:
                    SwitchControl(control.Control, path);
                    break;
                case SceneTrigger scene:
                    if (_environment.FindLogicOutput(scene.Scene, SceneTrigger.ActivatedOutputKey) == null)
                    {
                        Error(path, "sceneNotFound");
                    }
                    break;
            }
        }

        private void ValidateCondition(ConditionDefinition condition, string path)
        {
            switch (condition)
            {
                case null:
                    return;
                case ConditionGroup group:
                    for (var i = 0; i < (group.Conditions?.Count ?? 0); i++)
                    {
                        ValidateCondition(group.Conditions[i], $"{path}/{i}");
                    }
                    break;
                case DatapointCondition datapoint:
                    ReadableNode(datapoint.Node, path);
                    if (AutomationValues.FromJson(datapoint.Value) == null || (datapoint.Operator == CompareOperator.Between && AutomationValues.FromJson(datapoint.Value2) == null))
                    {
                        Error(path, "valueMissing");
                    }
                    break;
                case TimeCondition time:
                    if (!string.IsNullOrWhiteSpace(time.After) && !AutomationValues.TryParseTime(time.After, out _))
                    {
                        Error(path, "invalidTime", time.After);
                    }
                    if (!string.IsNullOrWhiteSpace(time.Before) && !AutomationValues.TryParseTime(time.Before, out _))
                    {
                        Error(path, "invalidTime", time.Before);
                    }
                    break;
                case SunCondition:
                    Location(path);
                    break;
                case ControlCondition control:
                    SwitchControl(control.Control, path);
                    break;
                case ShadingCondition shading:
                    ShadingBlind(shading.Control, path);
                    break;
                case OperatingModeCondition operatingMode:
                    if (string.IsNullOrWhiteSpace(operatingMode.Mode))
                    {
                        Error(path, "valueMissing");
                    }
                    else if (_environment.IsOperatingModeActive(operatingMode.Mode) == null)
                    {
                        Error(path, "operatingModeNotFound", operatingMode.Mode);
                    }
                    break;
                case TriggeredByCondition triggeredBy when string.IsNullOrWhiteSpace(triggeredBy.TriggerId):
                    Error(path, "valueMissing");
                    break;
            }
        }

        private void ValidateActions(IReadOnlyList<ActionDefinition> actions, string basePath)
        {
            for (var i = 0; i < (actions?.Count ?? 0); i++)
            {
                var path = $"{basePath}/{i}";
                switch (actions[i])
                {
                    case SetValueAction setValue:
                        var node = _environment.GetNode(setValue.Node);
                        if (node == null)
                        {
                            Error(path, "nodeNotFound");
                        }
                        else if (!node.IsWriteable)
                        {
                            Error(path, "nodeNotWritable", node.Name);
                        }

                        if (setValue.Mode == SetValueMode.Value && AutomationValues.FromJson(setValue.Value) == null)
                        {
                            Error(path, "valueMissing");
                        }
                        if (setValue.Mode == SetValueMode.Copy)
                        {
                            if (setValue.Source == null)
                            {
                                Error(path, "valueMissing");
                            }
                            else
                            {
                                ReadableNode(setValue.Source.Value, path);
                            }
                        }
                        break;
                    case ControlAction control:
                        var instance = _environment.GetControl(control.Control);
                        if (instance == null)
                        {
                            Error(path, "controlNotFound");
                        }
                        else if (!Supports(instance, control.Command))
                        {
                            Error(path, "controlNotSupported", instance.Name);
                        }
                        break;
                    case GroupControlAction group:
                        if (group.Area == null && group.Category == null)
                        {
                            Error(path, "groupTargetMissing");
                        }
                        if (group.Area != null && !_environment.AreaExists(group.Area.Value))
                        {
                            Error(path, "areaNotFound");
                        }
                        if (group.Category != null && !_environment.CategoryExists(group.Category.Value))
                        {
                            Error(path, "categoryNotFound");
                        }
                        break;
                    case DelayAction delay:
                        if (delay.Seconds < 0 || delay.RandomToSeconds < 0)
                        {
                            Error(path, "negativeDuration");
                        }
                        break;
                    case WaitUntilAction wait:
                        if (wait.Condition == null)
                        {
                            Error(path, "valueMissing");
                        }
                        if (wait.ForSeconds < 0 || wait.TimeoutSeconds < 0)
                        {
                            Error(path, "negativeDuration");
                        }
                        ValidateCondition(wait.Condition, $"{path}/condition");
                        break;
                    case NotifyAction notify:
                        if (string.IsNullOrWhiteSpace(notify.Subject) && string.IsNullOrWhiteSpace(notify.Body))
                        {
                            Error(path, "valueMissing");
                        }
                        break;
                    case MailAction mail:
                        ValidateMail(mail, path);
                        break;
                    case IfAction ifAction:
                        if (ifAction.Condition == null)
                        {
                            Error(path, "valueMissing");
                        }
                        ValidateCondition(ifAction.Condition, $"{path}/condition");
                        ValidateActions(ifAction.Then, $"{path}/then");
                        ValidateActions(ifAction.Else, $"{path}/else");
                        break;
                    case RunAutomationAction run:
                        Automation(run.Automation, path);
                        break;
                    case EnableAutomationAction enable:
                        Automation(enable.Automation, path);
                        break;
                    case ShadingAction shading:
                        if ((shading.Controls == null || shading.Controls.Count == 0) && shading.Area == null && shading.Category == null)
                        {
                            Error(path, "groupTargetMissing");
                        }
                        foreach (var control in shading.Controls ?? new List<Guid>())
                        {
                            ShadingBlind(control, path);
                        }
                        if (shading.Area != null && !_environment.AreaExists(shading.Area.Value))
                        {
                            Error(path, "areaNotFound");
                        }
                        if (shading.Category != null && !_environment.CategoryExists(shading.Category.Value))
                        {
                            Error(path, "categoryNotFound");
                        }
                        break;
                }
            }
        }

        private void Automation(Guid id, string path)
        {
            if (_self.HasValue && id == _self.Value)
            {
                Warning(path, "selfReference");
            }
            else if (!_automations.Contains(id))
            {
                Error(path, "automationNotFound");
            }
        }

        private void ReadableNode(Guid id, string path)
        {
            var node = _environment.GetNode(id);
            if (node == null)
            {
                Error(path, "nodeNotFound");
            }
            else if (!node.IsReadable)
            {
                Error(path, "nodeNotReadable", node.Name);
            }
        }

        private void SwitchControl(Guid id, string path)
        {
            var control = _environment.GetControl(id);
            if (control == null)
            {
                Error(path, "controlNotFound");
            }
            else if (control is not ISwitch)
            {
                Error(path, "controlNotSupported", control.Name);
            }
        }

        private void ShadingBlind(Guid id, string path)
        {
            var control = _environment.GetControl(id);
            if (control == null)
            {
                Error(path, "controlNotFound");
            }
            else if (control is not IBlind)
            {
                Error(path, "controlNotSupported", control.Name);
            }
            else if (_environment.IsShading(id) == null)
            {
                Warning(path, "shadingNotConfigured", control.Name);
            }
        }

        private void Location(string path)
        {
            if (!_environment.HasLocation)
            {
                Warning(path, "locationMissing");
            }
        }

        internal static bool Supports(IControl control, ControlCommand command)
        {
            return command switch
            {
                ControlCommand.On or ControlCommand.Off or ControlCommand.Toggle => control is ISwitch,
                ControlCommand.Dim => control is IDimmer,
                _ => control is IBlind
            };
        }
    }

    /// <summary>
    /// The objects a definition uses, stored as <see cref="AutomationReference"/>
    /// </summary>
    public static class AutomationReferences
    {
        public static IReadOnlyList<(Guid Id, AutomationReferenceType Type, AutomationReferenceRole Role)> Collect(AutomationDefinition definition)
        {
            var result = new HashSet<(Guid, AutomationReferenceType, AutomationReferenceRole)>();

            foreach (var trigger in definition.Triggers)
            {
                switch (trigger)
                {
                    case DatapointTrigger datapoint:
                        result.Add((datapoint.Node, AutomationReferenceType.NodeInstance, AutomationReferenceRole.Trigger));
                        break;
                    case ControlTrigger control:
                        result.Add((control.Control, AutomationReferenceType.Control, AutomationReferenceRole.Trigger));
                        break;
                    case SceneTrigger scene:
                        result.Add((scene.Scene, AutomationReferenceType.Control, AutomationReferenceRole.Trigger));
                        break;
                    case ConditionsTrigger conditions:
                        Condition(conditions.Conditions, AutomationReferenceRole.Trigger, result);
                        break;
                }
            }

            Condition(definition.Conditions, AutomationReferenceRole.Condition, result);
            Actions(definition.Actions, result);

            return result.Where(a => a.Item1 != Guid.Empty).ToList();
        }

        private static void Condition(ConditionDefinition condition, AutomationReferenceRole role, ISet<(Guid, AutomationReferenceType, AutomationReferenceRole)> result)
        {
            switch (condition)
            {
                case ConditionGroup group:
                    foreach (var child in group.Conditions ?? new List<ConditionDefinition>())
                    {
                        Condition(child, role, result);
                    }
                    break;
                case DatapointCondition datapoint:
                    result.Add((datapoint.Node, AutomationReferenceType.NodeInstance, role));
                    break;
                case ControlCondition control:
                    result.Add((control.Control, AutomationReferenceType.Control, role));
                    break;
                case ShadingCondition shading:
                    result.Add((shading.Control, AutomationReferenceType.Control, role));
                    break;
            }
        }

        private static void Actions(IEnumerable<ActionDefinition> actions, ISet<(Guid, AutomationReferenceType, AutomationReferenceRole)> result)
        {
            foreach (var action in actions ?? Enumerable.Empty<ActionDefinition>())
            {
                switch (action)
                {
                    case SetValueAction setValue:
                        result.Add((setValue.Node, AutomationReferenceType.NodeInstance, AutomationReferenceRole.Action));
                        if (setValue.Source.HasValue)
                        {
                            result.Add((setValue.Source.Value, AutomationReferenceType.NodeInstance, AutomationReferenceRole.Action));
                        }
                        break;
                    case ControlAction control:
                        result.Add((control.Control, AutomationReferenceType.Control, AutomationReferenceRole.Action));
                        break;
                    case GroupControlAction group:
                        if (group.Area.HasValue)
                        {
                            result.Add((group.Area.Value, AutomationReferenceType.Area, AutomationReferenceRole.Action));
                        }
                        if (group.Category.HasValue)
                        {
                            result.Add((group.Category.Value, AutomationReferenceType.Category, AutomationReferenceRole.Action));
                        }
                        break;
                    case WaitUntilAction wait:
                        Condition(wait.Condition, AutomationReferenceRole.Action, result);
                        break;
                    case IfAction ifAction:
                        Condition(ifAction.Condition, AutomationReferenceRole.Action, result);
                        Actions(ifAction.Then, result);
                        Actions(ifAction.Else, result);
                        break;
                    case RunAutomationAction run:
                        result.Add((run.Automation, AutomationReferenceType.Automation, AutomationReferenceRole.Action));
                        break;
                    case EnableAutomationAction enable:
                        result.Add((enable.Automation, AutomationReferenceType.Automation, AutomationReferenceRole.Action));
                        break;
                    case ShadingAction shading:
                        foreach (var control in shading.Controls ?? new List<Guid>())
                        {
                            result.Add((control, AutomationReferenceType.Control, AutomationReferenceRole.Action));
                        }
                        if (shading.Area.HasValue)
                        {
                            result.Add((shading.Area.Value, AutomationReferenceType.Area, AutomationReferenceRole.Action));
                        }
                        if (shading.Category.HasValue)
                        {
                            result.Add((shading.Category.Value, AutomationReferenceType.Category, AutomationReferenceRole.Action));
                        }
                        break;
                }
            }
        }
    }
}
