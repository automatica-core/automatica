using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen1.Dtos
{
    public class ShellyCloudDto
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("connected")]
        public bool Connected { get; set; }
    }
}