using Automatica.Core.Driver;
using P3.Driver.HueBridgeSimulatorDriverFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.HueBridgeSimulator.DriverFactory.OnOff
{
    public class HueOnOffStateNode : HueBridgeNode
    {
        public HueOnOffStateNode(IDriverContext driverContext, HueBridgeDriver driver) : base(driverContext, driver)
        {
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            var parent = (HueOnOffNode)Parent;
            var boolValue = Convert.ToBoolean(value);

            Driver.SetLightState(parent.LightNumber, new HueBridge.Data.HueSwitchLightData(boolValue, boolValue ? 100 : 0));
            await writeContext.DispatchValue(value, token);
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }

    }
}
