using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Knx.Falcon;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.Dpt;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    /// <summary>
    /// Group address node for a datapoint type from <see cref="KnxDptCatalog"/> with a single value.
    /// </summary>
    internal class KnxCodecAttribute : KnxGroupAddress
    {
        private IKnxDptCodec _codec;
        private byte[] _lastData;

        internal KnxDptCatalogEntry Entry { get; }

        internal KnxCodecAttribute(IDriverContext driverContext, KnxDriver knxDriver, KnxDptCatalogEntry entry) : base(driverContext, knxDriver)
        {
            Entry = entry;
        }

        public override int ImplementationDptType => Entry.MainType;

        internal IKnxDptCodec Codec => _codec ??= Entry.CreateCodec(DptSubType);

        protected override object ConvertToDptValue(object value)
        {
            byte[] data;
            try
            {
                data = Encode(value);
            }
            catch (Exception e) when (e is ArgumentException or FormatException or InvalidCastException or OverflowException)
            {
                DriverContext.Logger.LogWarning($"{GroupAddress}: could not encode {value} as DPT {Entry.MainType}: {e.Message}");
                return null;
            }

            if (WriteOnlyIfChanged && _lastData != null && _lastData.SequenceEqual(data))
            {
                return null;
            }

            _lastData = data;
            return data;
        }

        protected virtual byte[] Encode(object value)
        {
            return Codec.Encode(new Dictionary<string, object> { { KnxDptCodec.SingleValueKey, value } }, KnxDptCodec.SingleValueKey);
        }

        protected override object GetCurrentValue()
        {
            return _lastData == null ? null : BitConverter.ToString(_lastData);
        }

        protected sealed override GroupValue ToGroupValue(object dptValue)
        {
            return KnxDptCatalog.ToGroupValue(Codec, (byte[])dptValue);
        }

        protected sealed override object FromGroupValue(GroupValue groupValue)
        {
            var data = groupValue.Value;
            var decoded = Codec.Decode(data);
            _lastData = data;
            return decoded;
        }

        protected override bool ValueRead(object value)
        {
            if (value is IReadOnlyDictionary<string, object> values && values.TryGetValue(KnxDptCodec.SingleValueKey, out var v))
            {
                DispatchRead(v);
            }

            return false; // already dispatched
        }
    }

    /// <summary>
    /// Group address node for a multi-field datapoint type: the node itself is a folder and every field is a child node.
    /// </summary>
    internal sealed class KnxCompositeCodecAttribute : KnxCodecAttribute
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, object> _state = new();
        private readonly Dictionary<string, KnxCodecFieldNode> _children = new();
        private string _changedKey;

        internal KnxCompositeCodecAttribute(IDriverContext driverContext, KnxDriver knxDriver, KnxDptCatalogEntry entry) : base(driverContext, knxDriver, entry)
        {
        }

        internal Task WriteField(string key, object value, IWriteContext writeContext, CancellationToken token)
        {
            lock (_lock)
            {
                _state[key] = value;
                _changedKey = key;
            }

            return Write(value, writeContext, token);
        }

        protected override byte[] Encode(object value)
        {
            lock (_lock)
            {
                var data = Codec.Encode(_state, _changedKey);

                // keep the other fields in sync with what is sent (e.g. hex colour => red/green/blue)
                foreach (var decoded in Codec.Decode(data))
                {
                    if (decoded.Key == _changedKey || Equals(_state.GetValueOrDefault(decoded.Key), decoded.Value))
                    {
                        continue;
                    }

                    _state[decoded.Key] = decoded.Value;
                    if (_children.TryGetValue(decoded.Key, out var child))
                    {
                        child.Dispatch(decoded.Value);
                    }
                }

                return data;
            }
        }

        protected override bool ValueRead(object value)
        {
            if (value is not IReadOnlyDictionary<string, object> values)
            {
                return false;
            }

            lock (_lock)
            {
                foreach (var field in values)
                {
                    _state[field.Key] = field.Value;
                    if (_children.TryGetValue(field.Key, out var child))
                    {
                        child.Dispatch(field.Value);
                    }
                }
            }

            return false;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        internal Task<bool> ReadFromBus(CancellationToken token)
        {
            return Driver.Read(GroupAddress);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var child = Entry.Children.SingleOrDefault(a => a.TemplateKey(Entry) == ctx.NodeInstance.This2NodeTemplateNavigation.Key);
            if (child == null)
            {
                return null;
            }

            var node = new KnxCodecFieldNode(ctx, this, child.FieldKey);
            _children[child.FieldKey] = node;
            return node;
        }
    }

    internal sealed class KnxCodecFieldNode : DriverBase
    {
        private readonly KnxCompositeCodecAttribute _parent;

        public string FieldKey { get; }

        public KnxCodecFieldNode(IDriverContext driverContext, KnxCompositeCodecAttribute parent, string fieldKey) : base(driverContext)
        {
            _parent = parent;
            FieldKey = fieldKey;
        }

        internal void Dispatch(object value)
        {
            DispatchRead(value);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return _parent.WriteField(FieldKey, value, writeContext, token);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            if (DriverContext.NodeInstance.IsReadable)
            {
                await _parent.ReadFromBus(token);
            }

            return false;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
