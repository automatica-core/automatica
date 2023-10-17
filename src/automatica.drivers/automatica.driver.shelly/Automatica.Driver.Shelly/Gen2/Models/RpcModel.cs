using Newtonsoft.Json;
using System.Collections.Generic;

namespace Automatica.Driver.Shelly.Gen2.Models
{
    internal class RpcModel
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("src")] 
        public string Source { get; set; }

        [JsonProperty("params")]
        public Dictionary<string, object> Params { get; set; }

        [JsonProperty("auth", NullValueHandling = NullValueHandling.Ignore)]
        public AuthModel Auth { get; set; }
    }
}
