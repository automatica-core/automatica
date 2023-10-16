using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen1.Dtos
{
    public class RelayDto
    {
        [JsonProperty("ison")]
        public bool IsOn { get; set; }

        [JsonProperty("has_timer")]
        public bool HasTimer { get; set; }

        [JsonProperty("timer_remaining")]
        public long TimerRemaining { get; set; }
    }
}