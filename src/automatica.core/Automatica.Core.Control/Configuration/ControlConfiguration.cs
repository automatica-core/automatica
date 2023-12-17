using Automatica.Core.Control.Base;
using Newtonsoft.Json;

namespace Automatica.Core.Control.Configuration
{
    public class ControlConfiguration
    {
        [JsonProperty("switches")]
        public List<ISwitch> Switches { get; set; } = new();

        [JsonProperty("dimmer")]
        public List<IDimmer> Dimmer { get; set; } = new();

        [JsonProperty("blinds")]
        public List<IBlind> Blinds { get; set; } = new();
    }
}
