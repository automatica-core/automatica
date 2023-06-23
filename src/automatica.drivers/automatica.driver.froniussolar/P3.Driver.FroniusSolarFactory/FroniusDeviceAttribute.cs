using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using FroniusSolarClient;
using Microsoft.Extensions.Logging;
using P3.Driver.FroniusSolarFactory.Categories;

namespace P3.Driver.FroniusSolarFactory
{
    internal class FroniusDeviceAttribute : DriverBase
    {
        private List<FroniusCategoryAttribute> _attributes = new();
        private readonly System.Timers.Timer _pollTimer = new System.Timers.Timer();
        private SolarClient _client;


        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);
        private FroniusPollTimestampAttribute? _timestampAttribute;
        private FroniusDeviceStateAttribute? _deviceStateAttribute;

        internal FroniusDeviceStateAttribute? DeviceStateAttribute => _deviceStateAttribute;

        public int PollInterval { get; private set; }
        public byte DeviceId { get; private set; }

        public FroniusDeviceAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override bool Init()
        {
            PollInterval = GetPropertyValueInt("fronius-poll-interval");
            DeviceId = (byte)GetPropertyValueInt("fronius-device-id");
            IsDisabled = GetProperty("fronius-disabled")!.ValueBool!.Value;


            if (!IsDisabled)
            {
                _pollTimer.Interval = PollInterval;

                var ip = GetPropertyValueString("fronius-ip");

                if (!IPAddress.TryParse(ip, out var ipAddress))
                {
                    return false;
                }

                _client = new(ipAddress.ToString(), 1, DriverContext.Logger);
            }

            return base.Init();
        }

        public bool IsDisabled { get; set; }

        public override Task<bool> Start()
        {
            if (!IsDisabled)
            {
                if (_client == null)
                {
                    throw new ArgumentException("Init must be called before start..");
                }

                _pollTimer.Elapsed += PollTimerOnElapsed;
                _pollTimer.Start();

                PollAll().ConfigureAwait(false);
            }

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
                if (_client == null)
                {
                    throw new ArgumentException("Driver not initialized...");
                }
                foreach (var group in _attributes)
                {
                    await group.PollAttributes();
                }
            }
            finally
            {
                _waitSemaphore.Release(1);

            }
        }

        private async void PollTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                await PollAll();
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError($"Error polling inverter {ex}");
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;
            if (key == "last-poll-timestamp")
            {
                _timestampAttribute = new FroniusPollTimestampAttribute(ctx);
                return _timestampAttribute;
            }

            if (key == "device-state")
            {
                _deviceStateAttribute = new FroniusDeviceStateAttribute(ctx);
                return _deviceStateAttribute;
            }

            FroniusCategoryAttribute? attribute = null;
            switch (key)
            {
                case "fronius-solar-common-data":
                    attribute = new CommonInverterDataAttribute(ctx, _client, this);
                    break;
                case "fronius-solar-p3-inverter-data":
                    attribute = new P3InverterDataAttribute(ctx, _client, this);
                    break;
                case "fronius-solar-power-flow-realtime-data":
                    attribute = new PowerFlowRealtimeDataAttribute(ctx, _client, this);
                    break;
            }

            _attributes.Add(attribute);
            return attribute;
        }
    }
}
