using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Dtos
{
    public class MQTTDto
    {
        [JsonProperty("connected")]
        public bool Connected { get; set; }
    }
}