using System.Text.Json;
using System.Text.Json.Nodes;

namespace Automatica.Core.Automation.Definition
{
    /// <summary>
    /// A block of a released automation whose values visualization users may change
    /// </summary>
    /// <param name="Path">Path into the definition (see README, "Paths")</param>
    /// <param name="Kind">trigger, condition or action</param>
    /// <param name="Type">The block type ("time", "datapoint", ...)</param>
    /// <param name="Block">The block itself</param>
    /// <param name="Values">The current values of the fields users may change (camelCase JSON)</param>
    public sealed record UserEditableBlock(string Path, string Kind, string Type, object Block, JsonObject Values)
    {
        public IReadOnlyCollection<string> Fields => AutomationUserAccess.FieldsOf(Kind, Type);
    }

    /// <summary>
    /// New values of one block from a visualization user
    /// </summary>
    public sealed class UserBlockEdit
    {
        public string Path { get; set; }
        public JsonObject Values { get; set; }
    }

    /// <summary>
    /// What visualization users may change in an automation released for them (<see cref="AutomationDefinition.UserAccess"/>):
    /// only the fields below, and only in blocks the admin marked <c>userEditable</c>. Triggers, conditions and actions
    /// can neither be added, removed nor retargeted (node, control, operator stay as the admin set them).
    /// </summary>
    public static class AutomationUserAccess
    {
        public const string Trigger = "trigger";
        public const string Condition = "condition";
        public const string Action = "action";

        private static readonly Dictionary<(string Kind, string Type), string[]> EditableFields = new()
        {
            [(Trigger, "time")] = new[] { "at", "days" },
            [(Trigger, "sun")] = new[] { "event", "offsetMinutes", "days" },
            [(Trigger, "datapoint")] = new[] { "value", "forSeconds" },
            [(Condition, "time")] = new[] { "after", "before", "days" },
            [(Condition, "sun")] = new[] { "after", "afterOffsetMinutes", "before", "beforeOffsetMinutes" },
            [(Condition, "datapoint")] = new[] { "value", "value2" },
            [(Action, "setValue")] = new[] { "value", "step" },
            [(Action, "control")] = new[] { "value" },
            [(Action, "delay")] = new[] { "seconds", "randomToSeconds" },
            [(Action, "waitUntil")] = new[] { "forSeconds", "timeoutSeconds" },
            [(Action, "mail")] = new[] { "to" }
        };

        /// <summary>
        /// The fields users may change in a block of that kind and type, empty when the type cannot be released
        /// </summary>
        public static IReadOnlyCollection<string> FieldsOf(string kind, string type) =>
            type != null && EditableFields.TryGetValue((kind, type), out var fields) ? fields : Array.Empty<string>();

        public static bool CanBeUserEditable(string kind, string type) => FieldsOf(kind, type).Count > 0;

        /// <summary>
        /// The blocks users may change, in definition order. Empty when the automation is not released.
        /// </summary>
        public static IReadOnlyList<UserEditableBlock> Collect(AutomationDefinition definition)
        {
            var result = new List<UserEditableBlock>();
            if (definition is not { UserAccess: true })
            {
                return result;
            }

            var root = JsonNode.Parse(AutomationJson.Serialize(definition))!.AsObject();

            void Add(string path, string kind, object block, JsonObject json)
            {
                var type = json?["type"]?.GetValue<string>();
                if (json?["userEditable"]?.GetValue<bool>() != true || !CanBeUserEditable(kind, type))
                {
                    return;
                }

                var values = new JsonObject();
                foreach (var field in FieldsOf(kind, type))
                {
                    values[field] = json[field]?.DeepClone();
                }
                result.Add(new UserEditableBlock(path, kind, type, block, values));
            }

            void Conditions(ConditionDefinition condition, JsonObject json, string path)
            {
                if (condition is ConditionGroup group)
                {
                    var children = json["conditions"]?.AsArray();
                    for (var i = 0; i < group.Conditions.Count && children != null && i < children.Count; i++)
                    {
                        Conditions(group.Conditions[i], children[i]?.AsObject(), $"{path}/{i}");
                    }
                    return;
                }
                Add(path, Condition, condition, json);
            }

            void Actions(List<ActionDefinition> actions, JsonArray json, string basePath)
            {
                for (var i = 0; i < actions.Count && json != null && i < json.Count; i++)
                {
                    var path = $"{basePath}/{i}";
                    var action = json[i]?.AsObject();
                    Add(path, Action, actions[i], action);
                    switch (actions[i])
                    {
                        case IfAction ifAction:
                            if (ifAction.Condition != null)
                            {
                                Conditions(ifAction.Condition, action?["condition"]?.AsObject(), $"{path}/condition");
                            }
                            Actions(ifAction.Then, action?["then"]?.AsArray(), $"{path}/then");
                            Actions(ifAction.Else, action?["else"]?.AsArray(), $"{path}/else");
                            break;
                        case WaitUntilAction wait when wait.Condition != null:
                            Conditions(wait.Condition, action?["condition"]?.AsObject(), $"{path}/condition");
                            break;
                    }
                }
            }

            var triggers = root["triggers"]?.AsArray();
            for (var i = 0; i < definition.Triggers.Count && triggers != null && i < triggers.Count; i++)
            {
                var json = triggers[i]?.AsObject();
                Add($"triggers/{i}", Trigger, definition.Triggers[i], json);
                if (definition.Triggers[i] is ConditionsTrigger conditionsTrigger)
                {
                    Conditions(conditionsTrigger.Conditions, json?["conditions"]?.AsObject(), $"triggers/{i}/conditions");
                }
            }

            var rootConditions = root["conditions"]?.AsObject()?["conditions"]?.AsArray();
            for (var i = 0; i < definition.Conditions.Conditions.Count && rootConditions != null && i < rootConditions.Count; i++)
            {
                Conditions(definition.Conditions.Conditions[i], rootConditions[i]?.AsObject(), $"conditions/{i}");
            }

            Actions(definition.Actions, root["actions"]?.AsArray(), "actions");
            return result;
        }

        /// <summary>
        /// Applies the edits of a user to a copy of <paramref name="current"/>. Every edit must point to a block in
        /// <see cref="Collect"/> and may only contain its fields.
        /// </summary>
        /// <param name="errors">What was refused (English, for the response), empty on success</param>
        /// <returns>The changed definition, null when anything was refused</returns>
        public static AutomationDefinition Apply(AutomationDefinition current, IEnumerable<UserBlockEdit> edits, out List<string> errors)
        {
            errors = new List<string>();
            var editable = Collect(current).ToDictionary(a => a.Path, StringComparer.Ordinal);
            var root = JsonNode.Parse(AutomationJson.Serialize(current))!.AsObject();

            foreach (var edit in edits ?? Enumerable.Empty<UserBlockEdit>())
            {
                if (edit?.Path == null || !editable.TryGetValue(edit.Path, out var block))
                {
                    errors.Add($"{edit?.Path ?? "(no path)"}: not editable");
                    continue;
                }

                var json = Resolve(root, edit.Path);
                if (json == null)
                {
                    errors.Add($"{edit.Path}: not found");
                    continue;
                }

                foreach (var (field, value) in edit.Values ?? new JsonObject())
                {
                    if (!block.Fields.Contains(field))
                    {
                        errors.Add($"{edit.Path}: {field} is not editable");
                        continue;
                    }
                    json[field] = value?.DeepClone();
                }
            }

            if (errors.Count > 0)
            {
                return null;
            }

            try
            {
                var result = AutomationJson.Parse(root.ToJsonString());
                // the release and the marks are not the user's to change
                result.UserAccess = current.UserAccess;
                return result;
            }
            catch (AutomationDefinitionException e)
            {
                errors.Add(e.Message);
                return null;
            }
        }

        /// <summary>
        /// The JSON object of a block by its path
        /// </summary>
        public static JsonObject Resolve(JsonObject root, string path)
        {
            var segments = path.Split('/');
            JsonNode current;
            var i = 1;
            switch (segments[0])
            {
                case "triggers":
                    current = Index(root["triggers"], segments, ref i);
                    break;
                case "conditions":
                    current = root["conditions"];
                    break;
                case "actions":
                    current = Index(root["actions"], segments, ref i);
                    break;
                default:
                    return null;
            }

            while (current != null && i < segments.Length)
            {
                var segment = segments[i];
                if (int.TryParse(segment, out _))
                {
                    // child of a condition group
                    current = Index(current["conditions"], segments, ref i);
                }
                else if (segment is "then" or "else")
                {
                    i++;
                    current = Index(current[segment], segments, ref i);
                }
                else if (segment is "condition" or "conditions")
                {
                    current = current[segment];
                    i++;
                }
                else
                {
                    return null;
                }
            }

            return current as JsonObject;
        }

        private static JsonNode Index(JsonNode array, string[] segments, ref int i)
        {
            if (array is not JsonArray list || i >= segments.Length || !int.TryParse(segments[i], out var index) || index < 0 || index >= list.Count)
            {
                return null;
            }
            i++;
            return list[index];
        }
    }
}
