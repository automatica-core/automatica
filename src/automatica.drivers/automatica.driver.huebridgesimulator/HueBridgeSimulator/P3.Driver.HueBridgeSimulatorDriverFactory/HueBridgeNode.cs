using Automatica.Core.Driver;
using P3.Driver.HueBridge.EventArgs;

namespace P3.Driver.HueBridgeSimulatorDriverFactory
{
    public abstract class HueBridgeNode : DriverBase
    {
        public HueBridgeDriver Driver { get; }

        public HueBridgeNode(IDriverContext driverContext, HueBridgeDriver driver) : base(driverContext)
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
