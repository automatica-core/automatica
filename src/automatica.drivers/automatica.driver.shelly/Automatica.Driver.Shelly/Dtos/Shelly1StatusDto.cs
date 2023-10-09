using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Dtos
{
    /// <summary>
    /// https://shelly-api-docs.shelly.cloud/gen1/#status
    /// </summary>
    public class Shelly1StatusDto
    {
        [JsonProperty("wifi_sta")]
        public WiFiStatusDto WiFiStatus { get; set; }
        
        [JsonProperty("cloud")]
        public ShellyCloudDto ShellyCloud { get; set; }

        [JsonProperty("mqtt")]
        public MQTTDto Mqtt { get; set; }
        
        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("unixtime")]
        public long UnixTime { get; set; }
        
        [JsonProperty("serial")]
        public long Serial { get; set; }
        
        [JsonProperty("has_update")]
        public bool HasUpdate { get; set; }

        [JsonProperty("mac")]
        public string MacAddress { get; set; }
        
        [JsonProperty("update")]
        public ShellyUpdateDto Update { get; set; }
        
        [JsonProperty("ram_total")]
        public long RamTotal { get; set; }

        [JsonProperty("ram_free")]
        public long RamFree { get; set; }

        // ram_lwm

        [JsonProperty("fs_size")]
        public long FileSystemSize { get; set; }

        [JsonProperty("fs_free")]
        public long FileSystemFree { get; set; }
                
        [JsonProperty("uptime")]
        public long Uptime { get; set; }
    }
}