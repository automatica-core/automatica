using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen1.Dtos
{
    public class MQTTDto
    {
        [JsonProperty("connected")]
        public bool Connected { get; set; }
    }
}