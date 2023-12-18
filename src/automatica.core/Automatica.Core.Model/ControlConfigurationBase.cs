using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Automatica.Core.Model
{
    public class ControlConfigurationBase : TypedObject
    {
        [JsonProperty("switches")]
        public List<Guid> Switches { get; set; } = new();

        [JsonProperty("dimmer")]
        public List<Guid> Dimmer { get; set; } = new();

        [JsonProperty("blinds")]
        public List<Guid> Blinds { get; set; } = new();
    }
}
