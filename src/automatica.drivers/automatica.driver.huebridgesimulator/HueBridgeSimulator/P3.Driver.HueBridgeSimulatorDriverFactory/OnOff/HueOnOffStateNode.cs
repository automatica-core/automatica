using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.HueBridgeSimulatorDriverFactory;
using System;
using System.Threading.Tasks;

namespace P3.Driver.HueBridgeSimulator.DriverFactory.OnOff
{
    public class HueOnOffStateNode : HueBridgeNode
    {
        public HueOnOffStateNode(IDriverContext driverContext, HueBridgeDriver driver) : base(driverContext, driver)
        {
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var parent = (HueOnOffNode)Parent;
            var boolValue = Convert.ToBoolean(value);

            Driver.SetLightState(parent.LightNumber, new HueBridge.Data.HueSwitchLightData(boolValue, boolValue ? 100 : 0));
            DispatchValue(value);
            return base.WriteValue(source, value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }

    }
}
