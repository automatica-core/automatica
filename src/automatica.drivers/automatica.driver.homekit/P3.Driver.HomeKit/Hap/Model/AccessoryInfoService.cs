using Newtonsoft.Json;

namespace P3.Driver.HomeKit.Hap.Model
{
    public class AccessoryInfoService : Service
    {
        internal AccessoryInfoService(Accessory accessory) : base(accessory)
        {
        }

        [JsonIgnore]
        public Characteristic Name { get; set; }
        
        [JsonIgnore]
        public Characteristic Serial { get; set; }
    }
}
