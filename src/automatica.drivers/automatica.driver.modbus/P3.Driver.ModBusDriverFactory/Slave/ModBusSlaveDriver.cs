﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriver.Slave.Tcp;

namespace P3.Driver.ModBusDriverFactory.Slave
{
    public class ModBusSlaveDriver: DriverBase
    {
        private readonly bool _isTcp;
        private IModBusSlaveDriver _modBusDriver;

        private readonly List<ModBusSlaveDevice> _childs = new List<ModBusSlaveDevice>();

        public ModBusSlaveDriver(IDriverContext driverContext, bool isTcp=true) : base(driverContext)
        {
            _isTcp = isTcp;
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }

        public override bool Init()
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

                _modBusDriver = new ModBusSlaveTcpDriver(config, TelegramMonitor);
            }
            else
            {
                throw new NotImplementedException();
            }
            return base.Init();
        }


        public override Task<bool> Start()
        {
            Task.Run(() => {
                _modBusDriver?.Open();
            });
            return base.Start();
        }

        public override Task<bool> Stop()
        {
            _modBusDriver?.Close();
            return base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var dev = new ModBusSlaveDevice(ctx, _modBusDriver);

            _childs.Add(dev);
            return dev;
        }
    }
}
