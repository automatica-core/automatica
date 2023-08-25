using Automatica.Core.Driver;
using P3.Driver.HueBridge.EventArgs;
using P3.Driver.HueBridgeSimulatorDriverFactory;

namespace P3.Driver.HueBridgeSimulator.DriverFactory
{
    public abstract class HueBridgeNode : DriverNoneAttributeBase
    {
        public HueBridgeDriver Driver { get; }

        protected HueBridgeNode(IDriverContext driverContext, HueBridgeDriver driver) : base(driverContext)
        {
            Driver = driver;
        }
        

        public virtual void SwitchLight(HueSwitchLightEventArgs eventArgs)
        {

        }
    }
}
