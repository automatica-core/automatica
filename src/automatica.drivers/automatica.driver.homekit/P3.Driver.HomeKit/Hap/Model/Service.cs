using System.Collections.Generic;
using Newtonsoft.Json;

namespace P3.Driver.HomeKit.Hap.Model
{
    public class Service
    {
        [JsonIgnore]
        public Accessory Accessory { get; }

        internal Service(Accessory accessory)
        {
            Accessory = accessory;
            Characteristics = new List<Characteristic>();
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("iid")]
        public int Id { get; set; }

        [JsonProperty("characteristics")]
        public List<Characteristic> Characteristics { get; set; }
    }
}
