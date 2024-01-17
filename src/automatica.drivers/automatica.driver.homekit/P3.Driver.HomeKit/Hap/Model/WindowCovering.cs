using Newtonsoft.Json;

namespace P3.Driver.HomeKit.Hap.Model
{
    public class WindowCovering : Accessory
    {
        internal WindowCovering()
        {
            
        }

        [JsonIgnore]
        public Characteristic CurrentPosition { get; internal set; }

        [JsonIgnore]
        public Characteristic TargetPosition { get; internal set; }

        [JsonIgnore]
        public Characteristic PositionType { get; internal set; }
    }
}
