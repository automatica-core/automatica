using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices.Light
{
    public class IkeaTradfriLightColorNode : IkeaTradfriAttribute
    {
        private string _value;

        public IkeaTradfriLightColorNode(IDriverContext driverContext, IkeaTradfriContainerNode container) : base(driverContext, container, DeviceType.Light)
        {
        }

        internal override void Update(TradfriDevice device)
        {
            _value = device.LightControl[0].ColorHex;
            DispatchRead(_value);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await readContext.DispatchValue(_value, token);
            return true;
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            var strValue = Convert.ToString(value);

            _value = strValue;

            await Container.Gateway.Driver.SetColor(Container.DeviceId, strValue);
            await writeContext.DispatchValue(strValue, token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
