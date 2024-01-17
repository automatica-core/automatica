using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen2.Models;

public class Temperature
{
    [JsonProperty("tC")]
    public double TC { get; set; }

    [JsonProperty("tF")]
    public double TF { get; set; }
}