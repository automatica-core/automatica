using Newtonsoft.Json;

namespace P3.Driver.Loxone.Miniserver.Driver.Data
{
    public class LoxoneApiResponse<T> where T : LoxoneApiResponseLL
    {
        [JsonProperty("LL")]
        public T Data { get; set; }
    }
}
