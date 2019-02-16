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

        [JsonProperty("on")]
        public bool On { get; set; }
        public int Bri { get; set; }
    }
}
