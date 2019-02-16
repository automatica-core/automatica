using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.LoxApp
{
    public class LoxApp3Data
    {
        public DateTime LastModified { get; set; }

        [JsonProperty("msInfo")]
        public MiniserverInfo MiniserverInfo { get; set; }

        [JsonProperty("rooms")]
        public Dictionary<string, LoxoneRoom> Rooms { get; set; }

        [JsonProperty("cats")]
        public Dictionary<string, LoxoneCategory> Categories { get; set; }

        [JsonProperty("controls")]
        public Dictionary<string, LoxoneControl> Controls { get; set; }
    }
}
