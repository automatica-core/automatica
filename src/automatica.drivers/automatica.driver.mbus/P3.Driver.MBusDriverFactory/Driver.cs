using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Udp;

namespace P3.Driver.MBusDriverFactory
{
    public enum MBusType
    {
        Serial,
        Udp
    }
    public class Driver : DriverBase
    {
        private readonly MBusType _connectionType;
        private MBusConnection _connection;

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;

        public Driver(IDriverContext ctx, MBusType connection) : base(ctx)
        {
            _connectionType = connection;
            _logger = ctx.Logger;
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }
        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override bool Init()
        {
            if (_connectionType == MBusType.Serial)
            {
                return false;
            }
            else
            {
                var config = new MBusUdpConfig
                {
                    IpAddress = IPAddress.Parse(GetProperty("mbus-ip").ValueString),
                    Port = GetProperty("mbus-port").ValueInt.Value,
                    Timeout = GetProperty("mbus-timeout").ValueInt.Value
                };


                _connection = new MBusUdp(config, TelegramMonitor, _logger);

            }
            return true;
        }

        public async Task<MBusFrame> ScanDevice(int deviceId, int deviceTimeout)
        {
            await _lock.WaitAsync();
            try
            {
                return await _connection.ReadDevice(deviceId, false, deviceTimeout);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<MBusFrame> Read(Device device)
        {
            await _lock.WaitAsync();

            try
            {
                return await _connection.ReadDevice(device.DeviceId, device.ResetBeforeRead, device.DeviceTimeout);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError($"Device Read: {e}");
                return null;
            }
            finally
            {
                _lock.Release();
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new Device(ctx, this);
        }
    }
}
