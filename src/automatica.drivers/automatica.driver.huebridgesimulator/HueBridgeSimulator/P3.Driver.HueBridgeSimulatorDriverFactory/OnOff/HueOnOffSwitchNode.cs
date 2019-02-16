using Automatica.Core.Driver;
using P3.Driver.HueBridgeSimulatorDriverFactory;

namespace P3.Driver.HueBridgeSimulator.DriverFactory.OnOff
{
    public class HueOnOffSwitchNode : HueBridgeNode
    {
        public HueOnOffSwitchNode(IDriverContext driverContext, HueBridgeDriver driver) : base(driverContext, driver)
        {
            
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
