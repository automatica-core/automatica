using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automatica.Core.Automation.Definition
{
    /// <summary>
    /// Serializer settings of the definition and the run steps: camelCase properties, enums as camelCase strings.
    /// </summary>
    public static class AutomationJson
    {
        public static readonly JsonSerializerOptions Options = CreateOptions();

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowOutOfOrderMetadataProperties = true
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.MakeReadOnly(true);
            return options;
        }

        /// <exception cref="AutomationDefinitionException">The JSON is not a valid definition</exception>
        public static AutomationDefinition Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AutomationDefinition();
            }

            try
            {
                var definition = JsonSerializer.Deserialize<AutomationDefinition>(json, Options) ?? new AutomationDefinition();
                definition.Triggers ??= new();
                definition.Conditions ??= new();
                definition.Actions ??= new();
                return definition;
            }
            catch (JsonException e)
            {
                throw new AutomationDefinitionException($"Invalid automation definition: {e.Message}", e);
            }
            catch (NotSupportedException e)
            {
                throw new AutomationDefinitionException($"Invalid automation definition: {e.Message}", e);
            }
        }

        public static string Serialize(AutomationDefinition definition)
        {
            return JsonSerializer.Serialize(definition, Options);
        }
    }

    public class AutomationDefinitionException : Exception
    {
        public AutomationDefinitionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
