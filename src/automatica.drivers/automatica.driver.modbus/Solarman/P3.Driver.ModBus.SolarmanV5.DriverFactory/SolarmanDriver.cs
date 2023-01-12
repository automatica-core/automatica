using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBus.SolarmanV5.Config;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    internal class SolarmanDriver : DriverBase
    {
        private readonly DeviceMap _map;
        private readonly System.Timers.Timer _pollTimer = new System.Timers.Timer();
        public int PollInterval { get; private set; }
        public byte DeviceId { get; private set; }


        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);
        private SolarmanConnection? _driver;

        private readonly List<SolarmanGroupAttribute> _groups = new();
        private readonly Dictionary<string, SolarmanAttrribute> _nameMap = new();

        private SolarmanPollTimestampAttribute? _timestampAttribute;
        private string _ip;
        private int _port;
        private string _serial;

        internal SolarmanConnection Driver {
            get => _driver;
        } 

        internal SolarmanDriver(IDriverContext driverContext, DeviceMap map) : base(driverContext)
        {
            _map = map;
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }

        public override bool Init()
        {
            PollInterval = GetPropertyValueInt("solarman-poll-interval"); 
            DeviceId = (byte)GetPropertyValueInt("solarman-device-id");

            _ip = GetPropertyValueString("solarman-ip");
            _port = GetProperty("solarman-port")!.ValueInt!.Value;
            _serial = GetPropertyValueString("solarman-serial");

          

            _pollTimer.Interval = PollInterval;

            return base.Init();
        }

        private void Open()
        {
            _driver = new SolarmanConnection(new SolarmanConfig
            {
                IpAddress = IPAddress.Parse(_ip),
                Port = Convert.ToInt16(_port),
                SolarmanSerialNumber = Convert.ToUInt32(_serial),
                Timeout = 5000
            }, TelegramMonitor);
            _driver.Open();
        }

        public override Task<bool> Start()
        {
            Open();

            if (_driver == null)
            {
                throw new ArgumentException("Init must be called before start..");
            }

            _pollTimer.Elapsed += PollTimerOnElapsed;
            _pollTimer.Start();

            PollAll().ConfigureAwait(false);

            return base.Start();
        }

        private async Task PollAll()
        {
            if (_waitSemaphore.CurrentCount == 0)
            {
                return;
            }

            await _waitSemaphore.WaitAsync();
            _timestampAttribute?.DispatchTimestamp();
            try
            {
                if (_driver == null)
                {
                    throw new ArgumentException("Driver not initialized...");
                }
                foreach (var group in _groups)
                {
                    await group.PollAttributes();
                }
            }
            finally
            {
                _waitSemaphore.Release(1);

            }
        }

        private async void PollTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await PollAll();
            }
            catch (IOException ioe)
            {
                DriverContext.Logger.LogError(ioe,$"Error polling data, try to reconnect...{ioe}");
                await _driver!.Stop();
                Open();
            }
            catch(Exception ex)
            {
                DriverContext.Logger.LogError($"Could not poll solarman {ex}");
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;
            if (key == "last-poll-timestamp")
            {
                _timestampAttribute = new SolarmanPollTimestampAttribute(ctx);
                return _timestampAttribute;
            }

            var groupAttribute = new SolarmanGroupAttribute(ctx, _map, this, _nameMap);
            _groups.Add(groupAttribute);
            return groupAttribute;
            
        }

      
    }
}
