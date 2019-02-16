using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.NodeInstances.Nodes
{
    internal class BaseNode : DriverBase
    {
        public Characteristic Characteristic { get; }
        public HomeKitDriver Driver { get; }

        public BaseNode(IDriverContext driverContext, Characteristic characteristic, HomeKitDriver driver) : base(driverContext)
        {
            Characteristic = characteristic;
            Driver = driver;
        }

        public override Task<bool> Start()
        {
            return base.Start();
        }

        public sealed override bool Init()
        {
            Driver.RegisterCharacteristic(Characteristic.Service.Accessory, Characteristic, this);
            return base.Init();
        }

        internal virtual void SetValue(object value)
        {
            DispatchValue(value);
        }

        protected virtual object ConvertValue(object value)
        {
            return value;
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var writeValue = ConvertValue(value);
            Characteristic.Value = writeValue;
            Driver.WriteCharacteristic(Characteristic);

            return Task.FromResult(true);
        }

        public sealed override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
