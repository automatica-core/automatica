using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen2.Models
{
    internal class ResponseModel<T>
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; }

        [JsonProperty("result")]
        public T Result { get; set; }

        [JsonProperty("error")]
        public ErrorModel Error { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")] 
        public object Params { get; set; }
    }
}
