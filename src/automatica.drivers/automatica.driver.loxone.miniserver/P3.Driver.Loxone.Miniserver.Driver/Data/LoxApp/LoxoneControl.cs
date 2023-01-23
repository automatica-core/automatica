using Newtonsoft.Json;
using System.Collections.Generic;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.LoxApp
{
    public class LoxoneControl
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string UuidAction { get; set; }

        [JsonProperty("room")]
        public string RoomUuid { get; set; }

        [JsonProperty("cat")]
        public string CategoryUuid { get; set; }

        public int DefaultRating { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsSecured { get; set; }

        public Dictionary<string, object> States { get; set; }
    }
}
