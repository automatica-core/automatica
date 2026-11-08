using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.EBus;

namespace P3.Driver.EBusFactory.Wolf
{
    /// <summary>
    /// A Wolf BWL-1 on the bus: polls its registers and takes the values its heat generator master (address - 5) broadcasts
    /// </summary>
    internal sealed class WolfDevice : DriverNoneAttributeBase
    {
        private readonly EBusDriver _bus;
        private readonly List<EBusValueNode> _values = new();
        private bool _unreachableLogged;

        public WolfDevice(IDriverContext driverContext, EBusDriver bus) : base(driverContext)
        {
            _bus = bus;
        }

        internal byte Address { get; private set; } = WolfBwl1.DefaultAddress;
        internal IReadOnlyList<EBusValueNode> Values => _values;

        public override Task<bool> Init(CancellationToken token = default)
        {
            var address = GetPropertyValueInt(EBusFactory.DeviceAddressKey);
            if (address is < 0 or > 255 || !EBusProtocol.IsSlave((byte)address))
            {
                DriverContext.Logger.LogError($"{FullName}: {address} is not an eBUS slave address, using {WolfBwl1.DefaultAddress}");
                address = WolfBwl1.DefaultAddress;
            }

            Address = (byte)address;
            return base.Init(token);
        }

        internal async Task PollAsync(EBusConnection connection, CancellationToken token)
        {
            var failed = 0;
            var polled = 0;
            foreach (var node in _values.Where(v => v.DataPoint is { Source: WolfSource.Register }))
            {
                var dp = node.DataPoint;
                polled++;
                try
                {
                    var low = await connection.ReadWolfRegisterAsync(Address, dp.Register, token);
                    ushort? high = null;
                    if (dp.HighRegister.HasValue && low.HasValue)
                    {
                        high = await connection.ReadWolfRegisterAsync(Address, dp.HighRegister.Value, token);
                    }

                    if (!low.HasValue || (dp.HighRegister.HasValue && !high.HasValue))
                    {
                        node.NotAvailable();
                        continue;
                    }

                    node.Set(dp.Convert(low.Value, high));
                }
                catch (EBusException e)
                {
                    failed++;
                    DriverContext.Logger.LogDebug($"{FullName}: {e.Message}");
                }
            }

            if (polled > 0 && failed == polled)
            {
                if (!_unreachableLogged)
                {
                    _unreachableLogged = true;
                    DriverContext.Logger.LogWarning($"{FullName}: no answer from address 0x{Address:x2}");
                }
            }
            else
            {
                _unreachableLogged = false;
            }
        }

        internal void OnTelegram(EBusTelegram telegram)
        {
            if (telegram.Source != EBusProtocol.MasterOf(Address))
            {
                return;
            }

            if (EBusBroadcasts.TryDecodeHeatGeneratorStatus(telegram) is { } status)
            {
                foreach (var node in _values.Where(v => v.DataPoint?.Source == WolfSource.HeatGeneratorStatus))
                {
                    node.Set(node.DataPoint.FromStatus(status));
                }
            }
            else if (EBusBroadcasts.TryDecodeWolfFaultCode(telegram) is { } code)
            {
                foreach (var node in _values.Where(v => v.DataPoint?.Source == WolfSource.FaultCode))
                {
                    node.Set(code);
                }
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await _bus.PollAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            EBusValueNode node;
            if (key == WolfBwl1.CustomRegisterKey)
            {
                node = new WolfCustomRegisterNode(ctx, this);
            }
            else
            {
                var dataPoint = WolfBwl1.DataPoints.FirstOrDefault(d => d.Key == key);
                if (dataPoint == null)
                {
                    return null;
                }

                node = new EBusValueNode(ctx, this, dataPoint);
            }

            _values.Add(node);
            return node;
        }
    }

    /// <summary>
    /// A read only value, dispatched when it changes
    /// </summary>
    internal class EBusValueNode : DriverNotWriteableBase
    {
        private readonly WolfDevice _device;
        private readonly object _lock = new();
        private bool _hasValue;
        private bool _notAvailableLogged;

        public EBusValueNode(IDriverContext driverContext, WolfDevice device, WolfDataPoint dataPoint) : base(driverContext)
        {
            _device = device;
            DataPoint = dataPoint;
        }

        public WolfDataPoint DataPoint { get; protected set; }

        public object LastValue { get; private set; }

        public void Set(object value)
        {
            lock (_lock)
            {
                _notAvailableLogged = false;
                if (_hasValue && Equals(LastValue, value))
                {
                    return;
                }

                _hasValue = true;
                LastValue = value;
            }

            DispatchRead(value);
        }

        public void NotAvailable()
        {
            lock (_lock)
            {
                if (_notAvailableLogged)
                {
                    return;
                }

                _notAvailableLogged = true;
            }

            DriverContext.Logger.LogWarning($"{FullName}: the device does not have register {DataPoint.Register}");
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default) => _device.Read(token);

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }

    /// <summary>
    /// A register chosen by the user: value = raw (signed or unsigned 16 bit) * scale
    /// </summary>
    internal sealed class WolfCustomRegisterNode : EBusValueNode
    {
        public WolfCustomRegisterNode(IDriverContext driverContext, WolfDevice device) : base(driverContext, device, null)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            var register = GetPropertyValueInt(EBusFactory.RegisterKey);
            var scale = GetPropertyValueDouble(EBusFactory.ScaleKey);
            var signed = GetPropertyValueBool(EBusFactory.SignedKey) ?? false;
            DataPoint = new WolfDataPoint(WolfBwl1.CustomRegisterTemplate, WolfBwl1.CustomRegisterKey, "WOLF_REGISTER",
                Automatica.Core.Base.Templates.NodeDataType.Double, WolfSource.Register, (ushort)Math.Clamp(register, 0, ushort.MaxValue),
                Scale: scale == 0 ? 1 : scale, Signed: signed);
            return base.Init(token);
        }
    }
}
