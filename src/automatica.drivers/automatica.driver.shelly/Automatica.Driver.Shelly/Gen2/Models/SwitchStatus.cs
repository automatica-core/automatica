using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automatica.Driver.Shelly.Gen2.Models
{
    public class SwitchStatus
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("output")]
        public bool Output { get; set; }

        [JsonProperty("apower")]
        public double Apower { get; set; }

        [JsonProperty("voltage")]
        public double Voltage { get; set; }

        [JsonProperty("freq")]
        public double Freq { get; set; }

        [JsonProperty("current")]
        public double Current { get; set; }

        [JsonProperty("pf")]
        public double Pf { get; set; }

        [JsonProperty("aenergy")]
        public Aenergy Aenergy { get; set; }

        [JsonProperty("temperature")]
        public Temperature Temperature { get; set; }
    }
}
