#nullable enable annotations
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBus.SolarmanV5.Config;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    internal class SolarmanDriver : DriverNoneAttributeBase
    {
        private readonly DeviceMap _map;
        private readonly DeviceGroupMap _groupMap;
        private readonly System.Timers.Timer _pollTimer = new System.Timers.Timer();
        public int PollInterval { get; private set; }
        public byte DeviceId { get; private set; }


        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1);
        private SolarmanConnection? _driver;
        private volatile bool _stopped;

        private readonly List<SolarmanGroupAttribute> _groups = new();
        private readonly Dictionary<string, SolarmanAttrribute> _nameMap = new();

        private SolarmanPollTimestampAttribute? _timestampAttribute;
        private string _ip;
        private int _port;
        private string _serial;

        internal SolarmanConnection Driver {
            get => _driver;
        } 

        internal SolarmanDriver(IDriverContext driverContext, DeviceMap map, DeviceGroupMap groupMap) : base(driverContext)
        {
            _map = map;
            _groupMap = groupMap;
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            PollInterval = GetPropertyValueInt("solarman-poll-interval"); 
            DeviceId = (byte)GetPropertyValueInt("solarman-device-id");

            _ip = GetPropertyValueString("solarman-ip");
            _port = GetProperty("solarman-port")!.ValueInt!.Value;
            _serial = GetPropertyValueString("solarman-serial");

          

            _pollTimer.Interval = PollInterval;

            return base.Init(token);
        }

        private void Open()
        {
            _driver = new SolarmanConnection(new SolarmanConfig
            {
                IpAddress = IPAddress.Parse(_ip),
                // stored as a short like the modbus tcp config, ports > 32767 wrap
                Port = unchecked((short)_port),
                SolarmanSerialNumber = Convert.ToUInt32(_serial),
                Timeout = 5000
            }, TelegramMonitor, DriverContext.Logger);
            if (!_driver.Open())
            {
                _driver = null;

                DriverContext.Logger.LogError($"Could not open connection to {_ip}:{_port}");
            }
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _stopped = true;
            _pollTimer.Elapsed -= PollTimerOnElapsed;
            _pollTimer.Stop();
            _pollTimer.Dispose();

            try
            {
                var driver = _driver;
                if (driver != null)
                {
                    await driver.Stop();
                }
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not stop solarman connection {e}");
            }

            return await base.Stop(token);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _stopped = false;
            Open();

            if (_driver == null)
            {
                throw new ArgumentException("Init must be called before start..");
            }

            _pollTimer.Elapsed -= PollTimerOnElapsed;
            _pollTimer.Elapsed += PollTimerOnElapsed;
            _pollTimer.Start();

            PollAll().ConfigureAwait(false);

            return base.Start(token);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            PollAll().ConfigureAwait(false);

            return Task.FromResult(true);
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

                foreach (var groupRead in _groupMap.GroupRead)
                {
                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var value = await Driver.ReadRegisters(DeviceId, (ushort)groupRead.start,
                        groupRead.end - groupRead.start + 1, cancellationTokenSource.Token);

                    DriverContext.Logger.LogInformation($"Return status is {value.ModBusRequestStatus}");
                    if (value is { ModBusRequestStatus: ModBusRequestStatus.Success }
                        and ModBusRegisterValueReturn modbusRegisterValue)
                    {
                        // all groups share one name map, so one group dispatches every node of the range;
                        // calling every group dispatched each value once per group (7 times per poll on a full device)
                        var group = _groups.FirstOrDefault();
                        if (group != null)
                        {
                            await group.FetchValues(modbusRegisterValue, groupRead);
                        }
                    }

                }

                
            }
            catch (Exception e) when (e is SocketException or IOException or ArgumentException or OperationCanceledException)
            {
                DriverContext.Logger.LogError(e, $"Error polling data, try to reconnect...{e}");
                if (_driver != null)
                {
                    await _driver.Stop();
                }

                // do not reconnect once the driver was stopped
                if (!_stopped)
                {
                    Open();
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
            catch(Exception ex)
            {
                DriverContext.Logger.LogError($"Could not poll solarman {ex}");
            }
        }

        /// <summary>
        /// Sets or clears the grid charge bit of one time of use slot (<paramref name="slotIndex"/> 0-5) or of all slots (null).
        /// Only bit 0 of the charge registers is changed, the registers are read right before the write.
        /// </summary>
        /// <returns>The grid charge state the inverter reports after the write.</returns>
        internal async Task<bool> WriteGridCharge(int? slotIndex, bool enabled, SolarmanAttrribute source, CancellationToken token)
        {
            var (start, count) = _map.TimeOfUseChargeRegisters;
            if (count == 0)
            {
                throw new InvalidOperationException("The device has no time of use charge registers");
            }

            if (slotIndex is < 0 || slotIndex >= count)
            {
                throw new InvalidOperationException($"Time of use slot index {slotIndex} is out of range");
            }

            await _writeSemaphore.WaitAsync(token);
            try
            {
                var values = await SolarmanRegisterWriter.ReadModifyWrite(_driver!, DeviceId, (ushort)start, count,
                    (index, raw) => slotIndex == null || slotIndex == index ? SolarmanRegisterCodec.WithGridCharge(raw, enabled) : raw,
                    SolarmanRegisterCodec.GridChargeBit, _map.WritableRegisters, token);

                DriverContext.Logger.LogInformation($"Time of use charge registers {start}-{start + count - 1} now hold {string.Join(",", values.Select(a => $"0x{a:X4}"))}");

                // the other slots and the all-slots node follow right away instead of on the next poll;
                // all groups share one name map, so one group dispatches every node of the range
                var registerValues = new ModBusRegisterValueReturn(ModBusRequestStatus.Success, values, values);
                var group = _groups.FirstOrDefault();
                if (group != null)
                {
                    await group.FetchValues(registerValues, (start, start + count - 1), source);
                }

                return slotIndex.HasValue
                    ? SolarmanRegisterCodec.IsGridChargeEnabled(values[slotIndex.Value])
                    : SolarmanRegisterCodec.IsGridChargeEnabledInAnySlot(values);
            }
            finally
            {
                _writeSemaphore.Release();
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

            var groupAttribute = new SolarmanGroupAttribute(ctx, _map, _groupMap, this, _nameMap);
            _groups.Add(groupAttribute);
            return groupAttribute;
            
        }

      
    }
}
