using System.Globalization;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Matter.Driver
{
    /// <summary>A manually configured bridged device (folder node); its child nodes are the data points.</summary>
    internal sealed class MatterDeviceNode : DriverNoneAttributeBase
    {
        private readonly Dictionary<string, MatterPointNode> _points = new();

        public MatterDeviceNode(IDriverContext driverContext, MatterDeviceKind kind) : base(driverContext)
        {
            Kind = kind;
        }

        public MatterDeviceKind Kind { get; }

        public BridgedGenericDevice Device { get; private set; }

        /// <summary>Creates the bridged device for the data points that exist below this node.</summary>
        public BridgedGenericDevice CreateDevice()
        {
            var name = string.IsNullOrWhiteSpace(DriverContext.NodeInstance.Name) ? Kind.DisplayName : DriverContext.NodeInstance.Name;
            Device = new BridgedGenericDevice(DriverContext.NodeInstance.ObjId.ToString("N"), name, Kind, _points.Keys)
            {
                ValueRequested = OnValueRequested
            };

            foreach (var point in _points.Values)
            {
                point.Attach(Device);
            }

            return Device;
        }

        private Task OnValueRequested(string point, object value)
        {
            if (_points.TryGetValue(point, out var node))
            {
                node.DispatchFromMatter(value);
            }
            else
            {
                DriverContext.Logger.LogDebug("{Name}: no node for Matter data point {Point}", Name, point);
            }

            return Task.CompletedTask;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = MatterDeviceTemplates.PointForTemplate(Kind, ctx.NodeInstance.This2NodeTemplateNavigation?.Key);
            var definition = key == null ? null : Kind.GetPoint(key);
            if (definition == null)
            {
                return null;
            }

            var node = new MatterPointNode(ctx, definition);
            _points[key] = node;
            return node;
        }
    }

    /// <summary>A data point of a <see cref="MatterDeviceNode"/>.</summary>
    internal sealed class MatterPointNode : DriverBase
    {
        private readonly MatterDataPoint _point;
        private BridgedGenericDevice _device;
        private object _pending;
        private object _lastFromMatter;

        public MatterPointNode(IDriverContext driverContext, MatterDataPoint point) : base(driverContext)
        {
            _point = point;
        }

        internal void Attach(BridgedGenericDevice device)
        {
            _device = device;
            if (_pending != null)
            {
                device.SetValue(_point.Key, _pending);
                _pending = null;
            }
        }

        internal void DispatchFromMatter(object value)
        {
            var converted = Convert(value);
            _lastFromMatter = converted;
            DispatchRead(converted);
        }

        private object Convert(object value)
        {
            if (value == null)
            {
                return null;
            }

            return _point.Type switch
            {
                MatterValueType.Boolean => value is bool b ? b : System.Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0,
                MatterValueType.Integer => System.Convert.ToInt32(System.Convert.ToDouble(value, CultureInfo.InvariantCulture)),
                MatterValueType.Double => System.Convert.ToDouble(value, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (!_point.Direction.HasFlag(MatterPointDirection.ToMatter))
            {
                return Task.CompletedTask;
            }

            try
            {
                if (_device == null)
                {
                    _pending = value;
                }
                else
                {
                    _device.SetValue(_point.Key, value);
                }
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
            {
                DriverContext.Logger.LogWarning("{Name}: value {Value} is not valid for Matter data point {Point}: {Error}", Name, value, _point.Key, e.Message);
            }

            return Task.CompletedTask;
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            if (_lastFromMatter != null)
            {
                await readContext.DispatchValue(_lastFromMatter, token);
            }

            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
