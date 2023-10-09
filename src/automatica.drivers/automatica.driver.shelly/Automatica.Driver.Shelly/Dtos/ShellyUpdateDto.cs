using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Dtos
{
    public class ShellyUpdateDto
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    
        [JsonProperty("has_update")]
        public bool HasUpdate { get; set; }
    
        [JsonProperty("new_version")]
        public string NewVersion { get; set; }
    
        [JsonProperty("old_version")]
        public string OldVersion { get; set; }
    }
}