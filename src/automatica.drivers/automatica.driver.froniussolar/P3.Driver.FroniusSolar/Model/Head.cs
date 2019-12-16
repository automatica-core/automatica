using System;
using Newtonsoft.Json.Linq;

namespace P3.Driver.FroniusSolar.Model
{
    public sealed class Head
    {
        public JObject RequestArguments { get; set; }

        public Status Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
