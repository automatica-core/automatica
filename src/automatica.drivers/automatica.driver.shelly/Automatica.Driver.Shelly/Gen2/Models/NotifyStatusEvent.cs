using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Automatica.Driver.Shelly.Gen2.Models
{
    public class SwitchStatusEvent
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("apower")]
        public double Power { get; set; }

        [JsonProperty("current")]
        public double Current { get; set; }

        [JsonProperty("output")]
        public bool Output { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }

    public class NotifyStatusEvent
    {
        [JsonProperty("ts")]
        public double Ts { get; set; }

        [JsonProperty("switch:0")]
        public SwitchStatusEvent Switch0 { get; set; }

        [JsonProperty("switch:1")]
        public SwitchStatusEvent Switch1 { get; set; }

        public List<SwitchStatusEvent> Switches => [Switch0, Switch1];

        public T? GetValueFromSwitch<T>(int relayId, Expression<Func<SwitchStatusEvent, T?>> getValueExpression)
        {
            if (Switches.Count >= relayId)
            {
                var switchValue = GetSwitch(relayId);
                if(switchValue != null)
                    return getValueExpression.Compile().Invoke(GetSwitch(relayId));
            }

            return default;
        }

        public SwitchStatusEvent GetSwitch(int relayId)
        {
            if (Switches.Count > relayId)
            {
                return Switches[relayId];
            }

            return null;
        }
    }
}
