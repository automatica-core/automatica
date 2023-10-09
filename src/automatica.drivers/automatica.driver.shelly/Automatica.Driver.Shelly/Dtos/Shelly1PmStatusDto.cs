using Automatica.Driver.Shelly.Dtos.Shelly1PM;
using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Dtos
{
    /// <summary>
    /// Extended information for the Shelly1PM
    /// https://shelly-api-docs.shelly.cloud/gen1/#shelly1-1pm-status
    /// </summary>
    public class Shelly1PmStatusDto : Shelly1StatusDto
    {
        [JsonProperty("meters")]
        public MeterDto[] Meters { get; set; }
        
        [JsonProperty("relays")]
        public RelayDto[] Relays { get; set; }
    }
}