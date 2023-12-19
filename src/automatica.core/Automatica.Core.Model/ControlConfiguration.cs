using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Automatica.Core.Model
{
    public class ControlConfiguration : TypedObject
    {
        [JsonProperty("controls")]
        public List<Guid> Controls { get; set; } = new();
    }
}
