using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriver.Master.Rtu;
using P3.Driver.ModBusDriver.Master.Tcp;

namespace P3.Driver.ModBusDriverFactory.Master
{
    public class ModBusMasterDriver : DriverBase
    {
        private readonly bool _isTcp;
        private readonly List<ModBusMasterDevice> _childs = new List<ModBusMasterDevice>();
        private IModBusMasterDriver _modBusDriver;

        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);
        public ModBusMasterDriver(IDriverContext driverContext, bool isTcp=true) : base(driverContext)
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
                var ip = GetProperty("modbus-master-ip").ValueString;
                var port = GetProperty("modbus-master-port").ValueInt.Value;
                ModBus.Logger.LogInformation($"Connecting to {ip}:{port}...");

                _modBusDriver = new ModBusMasterTcpDriver(new ModBusMasterTcpConfig()
                {
                    IpAddress = IPAddress.Parse(ip),
                    Port = (short)port,
                    Timeout = 5000
                }, TelegramMonitor);
            }
            else
            {
                var baud = GetProperty("modbus-baudrate").ValueInt;
                var port = GetProperty("modbus-port").ValueString;
                var dataBits = GetProperty("modbus-databits").ValueInt;
                var stopBits = GetProperty("modbus-stopbits").ValueDouble;
                var parity = GetProperty("modbus-parity").ValueString;

                ModBus.Logger.LogInformation($"Connecting to {port} {baud} {dataBits} {parity} {stopBits}...");

                _modBusDriver = new ModBusMasterRtuDriver(new ModBusMasterRtuConfig()
                {
                    Port = port,
                    Baud = baud.Value,
                    DataBits = dataBits.Value,
                    Parity = parity,
                    StopBits = stopBits.Value,
                    Timeout = 5000
                }, TelegramMonitor);
            }
            return base.Init();
        }

        public override Task<bool> Start()
        {
            _modBusDriver.Open();
            return base.Start();
        }

        public override async Task<bool> Stop()
        {
            await _modBusDriver.Stop();
            return await base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var dev = new ModBusMasterDevice(ctx, _modBusDriver, _waitSemaphore);

            _childs.Add(dev);
            return dev;
        }
    }
}
