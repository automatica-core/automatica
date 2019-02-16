using System.Collections.Generic;
using Newtonsoft.Json;

namespace P3.Driver.HomeKit.Hap.Model
{
    public class AccessoryData
    {
        internal AccessoryData()
        {
            Accessories = new List<Accessory>();
        }

        [JsonProperty("accessories")]
        public List<Accessory> Accessories { get; set; }
    }
}
