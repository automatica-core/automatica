using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriver.Slave.Tcp;

namespace P3.Driver.ModBusDriverFactory.Slave
{
    public class ModBusSlaveDriver: DriverBase
    {
        private readonly ILogger _logger;
        private readonly bool _isTcp;
        private IModBusSlaveDriver _modBusDriver;

        private readonly List<ModBusSlaveDevice> _childs = new List<ModBusSlaveDevice>();

        public ModBusSlaveDriver(IDriverContext driverContext, ILogger logger, bool isTcp=true) : base(driverContext)
        {
            _logger = logger;
            _isTcp = isTcp;
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }
        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            if (_isTcp)
            {
                var port = GetProperty("modbus-port").ValueInt.Value;
                var ignoreDeviceId = GetProperty("modbus-ignoreDeviceId").ValueBool.Value;

                var devices = new List<int>();
                foreach (var ch in DriverContext.NodeInstance.InverseThis2ParentNodeInstanceNavigation)
                {
                    devices.Add(GetProperty(ch, "modbus-device-id").ValueInt.Value);
                }

                var config = new ModBusSlaveTcpConfig();
                config.Port = (ushort) port;
                config.IgnoreDeviceId = ignoreDeviceId;
                config.DeviceIds = devices;

                _modBusDriver = new ModBusSlaveTcpDriver(config, TelegramMonitor, _logger);
                ModBus.Logger = DriverContext.Logger;
            }
            else
            {
                throw new NotImplementedException();
            }
            return base.Init(token);
        }


        public override Task<bool> Start(CancellationToken token = default)
        {
            if (_modBusDriver != null)
            {
                DriverContext.Logger.LogInformation($"Starting modbus tcp...");
                _modBusDriver.Open();
            }
            else
            {
                DriverContext.Logger.LogInformation($"Something went wrong starting the modbus tcp slave...");
            }

            return base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _modBusDriver?.Close();
            return base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var dev = new ModBusSlaveDevice(ctx, _modBusDriver);

            _childs.Add(dev);
            return dev;
        }
    }
}
