using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using FroniusSolarClient;

namespace P3.Driver.FroniusSolarFactory
{
    internal abstract class FroniusCategoryAttribute : DriverBase
    {
        protected SolarClient SolarClient { get; }
        protected FroniusDeviceAttribute Device { get; }

        protected Dictionary<string, FroniusSolarValueAttribute> _attributes = new();

        protected FroniusDeviceStateAttribute DeviceStateAttribute => Device.DeviceStateAttribute;

        protected FroniusCategoryAttribute(IDriverContext driverContext, SolarClient solarClient, FroniusDeviceAttribute device) : base(driverContext)
        {
            SolarClient = solarClient;
            Device = device;
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;
            var valueAttribute =  new FroniusSolarValueAttribute(ctx);

            _attributes.Add(key, valueAttribute);

            return valueAttribute;
        }

        public abstract Task PollAttributes(CancellationToken token = default);
    }
}
