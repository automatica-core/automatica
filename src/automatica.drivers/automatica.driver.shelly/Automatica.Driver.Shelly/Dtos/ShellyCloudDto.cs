using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Dtos
{
    public class ShellyCloudDto
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("connected")]
        public bool Connected { get; set; }
    }
}