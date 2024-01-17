using System.Collections.Generic;
using Newtonsoft.Json;

namespace P3.Driver.HomeKit.Hap.Model
{
    public class Accessory
    {
        internal Accessory()
        {
            Services = new List<Service>();    
        }

        [JsonProperty("aid")]
        public ulong Id { get; set; }

        [JsonProperty("services")]
        public List<Service> Services { get; set; }

        [JsonIgnore]
        public AccessoryInfoService AccessoryInfo { get; set; }
        
        [JsonIgnore]
        public Service Specific { get; set; }
    }
}
