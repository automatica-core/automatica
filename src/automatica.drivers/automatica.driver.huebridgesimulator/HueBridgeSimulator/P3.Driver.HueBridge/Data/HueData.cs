using System.Collections.Generic;

namespace P3.Driver.HueBridge.Data
{
    public class HueData
    {
        public IDictionary<int, HueLight> Lights => HueDriver.Instance.Lights;

        public object Scenes => new object();
        public object Groups => new object();
        public object Schedules => new object();
        public object Sensors => new object();
        public object Rules => new object();

        public BridgeConfig Config => HueDriver.Instance.BridgeConfig;
    }
}
