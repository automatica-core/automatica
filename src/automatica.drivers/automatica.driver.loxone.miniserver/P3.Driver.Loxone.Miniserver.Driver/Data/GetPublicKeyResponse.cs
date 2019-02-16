using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.Loxone.Miniserver.Driver.Data
{
    public class GetPublicKeyResponse : LoxoneApiResponseLL
    {
        [JsonProperty("Value")]
        public string PublicKey { get; set; }
    }
}
