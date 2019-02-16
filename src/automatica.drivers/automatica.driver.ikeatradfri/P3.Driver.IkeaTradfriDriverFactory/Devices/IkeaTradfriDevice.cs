using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices
{
    public abstract class IkeaTradfriDevice : DriverBase
    {
        private readonly TradfriDeviceType _deviceType;
        internal IkeaTradfriContainerNode Container { get; }
        
        protected IkeaTradfriDevice(IDriverContext driverContext, IkeaTradfriContainerNode container, TradfriDeviceType deviceType) : base(driverContext)
        {
            _deviceType = deviceType;
            Container = container;
        }

        protected abstract void Update(JToken device);

        public override bool Init()
        {
         
            return base.Init();
        }


        public override Task<bool> Start()
        {
            Container.Gateway.Driver.RegisterChange(a =>
            {
                try
                {
                    Update(a);
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, "Could not update tradfri device state");
                }

            }, _deviceType, Container.DeviceId);
            return base.Start();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
