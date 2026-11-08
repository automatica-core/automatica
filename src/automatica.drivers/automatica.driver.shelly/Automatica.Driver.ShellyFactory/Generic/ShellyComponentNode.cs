#nullable enable annotations
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.ShellyFactory.Catalog;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.ShellyFactory.Generic
{
    /// <summary>
    /// One component instance of a device ("switch:0", Gen1 "relays"[0], the Gen1 root sensors...) with its data points.
    /// Keeps the last known status of the component, Gen2+ notifications only carry the changed fields.
    /// </summary>
    internal sealed class ShellyComponentNode : DriverBase
    {
        private readonly ShellyGenericDevice _device;
        private readonly object _lock = new();
        private JToken? _status;

        public ShellyComponentType ComponentType { get; }
        public int ComponentId { get; private set; }
        public List<ShellyDataPointNode> DataPoints { get; } = new();

        public ShellyComponentNode(IDriverContext driverContext, ShellyGenericDevice device, ShellyComponentType componentType) : base(driverContext)
        {
            _device = device;
            ComponentType = componentType;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            ComponentId = ComponentType.HasId ? GetPropertyValueInt(ShellyGenericTemplates.ComponentIdKey) : 0;
            return base.Init(token);
        }

        /// <summary>
        /// "switch:0" for Gen2+, the section for Gen1.
        /// </summary>
        public string Key => ComponentType.Key(ComponentId);

        public JToken? Status
        {
            get
            {
                lock (_lock)
                {
                    return _status?.DeepClone();
                }
            }
        }

        /// <summary>
        /// A complete device status (Shelly.GetStatus, NotifyFullStatus, Gen1 /status).
        /// </summary>
        public void ApplyFullStatus(JObject deviceStatus)
        {
            var component = ComponentType.Select(deviceStatus, ComponentId);
            if (component == null)
            {
                return;
            }

            lock (_lock)
            {
                _status = component.DeepClone();
            }

            DispatchFrom(component);
        }

        /// <summary>
        /// A NotifyStatus: only the changed fields of the changed components.
        /// </summary>
        public void ApplyPartialStatus(JObject partialStatus)
        {
            var component = ComponentType.Select(partialStatus, ComponentId);
            if (component == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_status is JObject current && component is JObject delta)
                {
                    current.Merge(delta, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge });
                }
                else
                {
                    _status = component.DeepClone();
                }
            }

            DispatchFrom(component);
        }

        /// <summary>
        /// The status of only this component, e.g. the answer of a Gen1 /relay/0?turn=on.
        /// </summary>
        public void ApplyComponentStatus(JToken component)
        {
            lock (_lock)
            {
                if (_status is JObject current && component is JObject delta)
                {
                    current.Merge(delta, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                }
                else
                {
                    _status = component.DeepClone();
                }
            }

            DispatchFrom(component);
        }

        private void DispatchFrom(JToken component)
        {
            foreach (var dp in DataPoints)
            {
                if (dp.DataPoint.IsCommand || dp.DataPoint.IsEvent)
                {
                    continue;
                }

                var token = dp.DataPoint.Select(component);
                if (token != null)
                {
                    dp.DispatchIfChanged(dp.DataPoint.Convert(token));
                }
            }
        }

        public void ApplyEvent(string eventName)
        {
            foreach (var dp in DataPoints.Where(a => a.DataPoint.IsEvent))
            {
                dp.DispatchEvent(eventName);
            }
        }

        /// <summary>
        /// A value of a Gen1 CoIoT status packet.
        /// </summary>
        public bool ApplyCoIoT(string description, JToken value)
        {
            var applied = false;
            foreach (var dp in DataPoints)
            {
                if (!dp.DataPoint.CoIoT.Contains(description))
                {
                    continue;
                }

                var converted = dp.DataPoint.Convert(value);
                if (converted == null)
                {
                    continue;
                }

                if (dp.DataPoint.IsEvent || dp.DataPoint.Type == Core.Base.Templates.NodeDataType.String && dp.DataPoint.Key == "event")
                {
                    if (converted is string s && s.Length > 0)
                    {
                        dp.DispatchEvent(s);
                    }
                }
                else
                {
                    dp.DispatchIfChanged(converted);
                }
                applied = true;
            }

            return applied;
        }

        internal Task WriteAsync(ShellyDataPoint dataPoint, object value, CancellationToken token)
        {
            return _device.WriteAsync(this, dataPoint, value, token);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = ComponentType.DataPoints.FirstOrDefault(a => a.Template == ctx.NodeInstance.This2NodeTemplate)
                            ?? (key == null ? null : ComponentType.GetDataPoint(key));
            if (dataPoint == null)
            {
                DriverContext.Logger.LogWarning($"Unknown data point {key} of shelly component {ComponentType.Type}");
                return null!;
            }

            var node = new ShellyDataPointNode(ctx, this, dataPoint);
            DataPoints.Add(node);
            return node;
        }
    }
}
