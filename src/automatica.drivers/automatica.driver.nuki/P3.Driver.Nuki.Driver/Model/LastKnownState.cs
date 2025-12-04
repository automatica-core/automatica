using Newtonsoft.Json;
using System;

namespace P3.Driver.Nuki.Driver.Model
{
    public class LastKnownState
    {
        [JsonProperty("mode")]
        public int Mode { get; set; }

        [JsonProperty("state")]
        public int State { get; set; }

        [JsonProperty("stateName")]
        public string StateName { get; set; }

        [JsonProperty("batteryCritical")]
        public bool BatteryCritical { get; set; }

        [JsonProperty("batteryCharging")]
        public bool BatteryCharging { get; set; }

        [JsonProperty("batteryChargeState")]
        public int BatteryChargeState { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
