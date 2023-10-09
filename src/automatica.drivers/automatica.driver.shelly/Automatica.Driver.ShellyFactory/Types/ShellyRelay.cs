using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Dtos.Shelly1PM;

namespace Automatica.Driver.ShellyFactory.Types
{
    internal class ShellyRelay : DriverBase
    {
        public ShellyDriverDevice ShellyDevice { get; }
        public int RelayId { get; }
        public ShellyRelay(IDriverContext driverContext, ShellyDriverDevice shellyDevice) : base(driverContext)
        {
            ShellyDevice = shellyDevice;
            RelayId = GetPropertyValueInt("shelly-relay-channel");
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            await ShellyDevice.Write(RelayId, Convert.ToBoolean(value), token);
            await writeContext.DispatchValue(value, token);
        }

        internal Task GetValueFromShelly(RelayDto relay)
        {
             DispatchRead(relay.IsOn);
             return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
