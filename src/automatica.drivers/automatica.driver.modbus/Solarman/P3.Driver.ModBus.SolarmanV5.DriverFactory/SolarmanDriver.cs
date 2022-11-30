﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
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

            var ip = GetPropertyValueString("solarman-ip");
            var port =GetProperty("solarman-port").ValueInt.Value;
            var serial = GetPropertyValueString("solarman-serial");

            _driver = new SolarmanConnection(new SolarmanConfig
            {
                IpAddress = IPAddress.Parse(ip),
                Port = Convert.ToInt16(port),
                SolarmanSerialNumber = Convert.ToUInt32(serial),
                Timeout = 5000
            }, TelegramMonitor);

            _pollTimer.Interval = 100;

            return base.Init();
        }

        public override Task<bool> Start()
        {
            if (_driver == null)
            {
                throw new ArgumentException("Init must be called before start..");
            }
            _driver.Open();
            _pollTimer.Elapsed += PollTimerOnElapsed;
            _pollTimer.Start();

            return base.Start();
        }

        private async void PollTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            await _waitSemaphore.WaitAsync();
            try
            {
                if (_driver == null)
                {
                    throw new ArgumentException("Driver not initialized...");
                }
                if (_driver.Connected)
                {
                    foreach (var group in _groups)
                    {
                        await group.PollAttributes();
                    }
                }
            }
            finally
            {
                _waitSemaphore.Release(1);

            }

        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var groupAttribute = new SolarmanGroupAttribute(ctx, _map, _driver, this, _nameMap);
            _groups.Add(groupAttribute);
            return groupAttribute;
            
        }

      
    }
}
