using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.HueBridge.Data
{
    public class HueGroupState
    {
        [JsonProperty("all_on")]
        public bool AllOn { get; set; }

        [JsonProperty("any_on")]
        public bool AnyOn { get; set; }
    }

    public class HueGroupData
    {
        public HueGroupAction Action { get; set; }

        public List<string> Lights { get; set; }

        public string Name { get; set; }
        public string Type => "LightGroup";

        public HueGroupState State { get; set; }
    }
}
