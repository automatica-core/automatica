using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.NodeInstances.Nodes
{
    internal class BaseNode : DriverBase
    {
        public Characteristic Characteristic { get; }
        public HomeKitDriver Driver { get; }

        internal BaseNode(IDriverContext driverContext, Characteristic characteristic, HomeKitDriver driver) : base(driverContext)
        {
            Characteristic = characteristic;
            Driver = driver;
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            return base.Start(token);
        }

        public sealed override Task<bool> Init(CancellationToken token = default)
        {
            Driver.RegisterCharacteristic(Characteristic.Service.Accessory, Characteristic, this);
            return base.Init(token);
        }

        internal virtual void SetValue(object value)
        {
            DispatchRead(value);
        }

        protected virtual object ConvertValue(object value)
        {
            return value;
        }
        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }
        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            var writeValue = ConvertValue(value);
            Characteristic.Value = writeValue;
            Driver.WriteCharacteristic(Characteristic);

            return Task.CompletedTask;
        }

        public sealed override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
