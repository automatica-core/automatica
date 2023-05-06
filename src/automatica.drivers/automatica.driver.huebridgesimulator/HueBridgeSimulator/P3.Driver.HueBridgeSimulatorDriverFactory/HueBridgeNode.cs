using Automatica.Core.Driver;
using P3.Driver.HueBridge.EventArgs;
using P3.Driver.HueBridgeSimulatorDriverFactory;

namespace P3.Driver.HueBridgeSimulator.DriverFactory
{
    public abstract class HueBridgeNode : DriverBase
    {
        public HueBridgeDriver Driver { get; }

        protected HueBridgeNode(IDriverContext driverContext, HueBridgeDriver driver) : base(driverContext)
        {
            Driver = driver;
        }

        public override bool Init()
        {
            return base.Init();
        }

        public virtual void SwitchLight(HueSwitchLightEventArgs eventArgs)
        {

        }
    }
}
