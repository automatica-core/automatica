using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Common
{
    public class ShellyInfoDto
    {
        [JsonProperty("mac")] 
        public string Mac { get; set; }
    }
}
