using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen1.Dtos
{
    public class RollerDto
    {
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("power")]
        public int Power { get; set; }

        [JsonProperty("is_valid")]
        public bool IsValid { get; set; }

        [JsonProperty("safety_switch")]
        public bool SafetySwitch { get; set; }

        [JsonProperty("overtemperature")]
        public bool OverTemperature { get; set; }

        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }

        [JsonProperty("last_direction")]
        public string LastDirection { get; set; }

        [JsonProperty("current_pos")]
        public int CurrentPos { get; set; }

        [JsonProperty("calibrating")]
        public bool Calibrating { get; set; }

        [JsonProperty("positioning")]
        public bool Positioning { get; set; }
    }
}
