using Automatica.Core.Control.Base;
using Newtonsoft.Json;

namespace Automatica.Core.Control.Configuration
{
    public class ControlConfiguration
    {
        [JsonProperty("controls")]
        public List<IControl> Controls { get; set; } = new();
    }
}
