using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automatica.Core.Shading.Definition
{
    /// <summary>
    /// Serializer settings of the shading documents: camelCase properties, enums as camelCase strings
    /// </summary>
    public static class ShadingJson
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

        /// <exception cref="ShadingDefinitionException">The JSON is not valid</exception>
        public static T Parse<T>(string json) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Normalize(new T());
            }

            try
            {
                return Normalize(JsonSerializer.Deserialize<T>(json, Options) ?? new T());
            }
            catch (Exception e) when (e is JsonException or NotSupportedException)
            {
                throw new ShadingDefinitionException($"Invalid shading definition: {e.Message}", e);
            }
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, Options);
        }

        private static T Normalize<T>(T value)
        {
            switch (value)
            {
                case ShadingSettingsDefinition settings:
                    settings.SunAzimuth = Normalize(settings.SunAzimuth) ;
                    settings.SunElevation = Normalize(settings.SunElevation);
                    settings.Brightness = Normalize(settings.Brightness);
                    settings.Wind = Normalize(settings.Wind);
                    settings.Rain = Normalize(settings.Rain);
                    settings.OutsideTemperature = Normalize(settings.OutsideTemperature);
                    settings.Thresholds ??= new SunThresholds();
                    settings.Winter ??= new WinterDefinition();
                    break;
                case FacadeDefinition facade:
                    facade.Brightness = Normalize(facade.Brightness);
                    break;
                case BlindDefinition blind:
                    blind.Slats ??= new SlatDefinition();
                    blind.RoomTemperature = Normalize(blind.RoomTemperature);
                    break;
            }

            return value;
        }

        private static Signal Normalize(Signal signal)
        {
            signal ??= new Signal();
            signal.Sources ??= new List<SignalSource>();
            signal.Sources.RemoveAll(a => a == null);
            return signal;
        }
    }

    public class ShadingDefinitionException : Exception
    {
        public ShadingDefinitionException(string message, Exception inner = null) : base(message, inner)
        {
        }
    }
}
