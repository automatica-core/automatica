using System.Collections.Generic;
using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen2.Models;

public class Aenergy
{
    [JsonProperty("total")]
    public double Total { get; set; }

    [JsonProperty("by_minute")]
    public List<double> ByMinute { get; set; }

    [JsonProperty("minute_ts")]
    public int MinuteTs { get; set; }
}