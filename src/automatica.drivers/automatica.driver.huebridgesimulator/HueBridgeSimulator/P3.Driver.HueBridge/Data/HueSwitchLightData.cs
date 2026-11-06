using Newtonsoft.Json;

namespace P3.Driver.HueBridge.Data
{
    public class HueSwitchLightData
    {
        public HueSwitchLightData()
        {

        }

        public HueSwitchLightData(bool on, int brightness)
        {
            On = on;
            Bri = brightness;
        }

        /// <summary>
        /// Null when the request does not contain "on" (e.g. a brightness-only request).
        /// </summary>
        [JsonProperty("on")]
        public bool? On { get; set; }

        [JsonProperty("bri")]
        public int? Bri { get; set; }
    }
}
