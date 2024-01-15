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

        public Service AccessoryInfo { get; set; }
        public Service Specific { get; set; }
    }
}
