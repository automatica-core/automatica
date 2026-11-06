using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Knx.Falcon.ApplicationData;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt2ValueAttribute : DriverBase
    {
        internal const int ValueIndex = 0;
        internal const int ControlIndex = 1;

        private readonly KnxDpt2Attribute _parent;

        public int Index { get; }
        public bool? Value { get; set; }

        public KnxDpt2ValueAttribute(IDriverContext driverContext, KnxDpt2Attribute parent, int index) : base(driverContext)
        {
            _parent = parent;
            Index = index;
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            var boolValue = Convert.ToBoolean(value);

            if (boolValue == Value)
            {
                await writeContext.DispatchValue(boolValue, token);
                return;
            }

            // the parent encodes the state of all children, so the new value must be set before writing
            var oldValue = Value;
            Value = boolValue;

            try
            {
                await _parent.WriteInternal(boolValue, writeContext, token); // dispatches the value on success
            }
            catch
            {
                Value = oldValue;
                throw;
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            if (DriverContext.NodeInstance.IsReadable)
            {
                await _parent.ReadFromBus();
            }

            return false;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }

    public class KnxDpt2Attribute : KnxGroupAddress
    {
        private readonly List<KnxDpt2ValueAttribute> _children = new List<KnxDpt2ValueAttribute>();
        public KnxDpt2Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }
        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt2;

        protected override object ConvertToDptValue(object value)
        {
            var controlAttribute = _children.FirstOrDefault(a => a.Index == KnxDpt2ValueAttribute.ControlIndex);
            var valueAttribute = _children.FirstOrDefault(a => a.Index == KnxDpt2ValueAttribute.ValueIndex);

            var controlValue = controlAttribute?.Value ?? false;
            var valueValue = valueAttribute?.Value ?? false;

            var dpt2Value = new Knx1BitControlled(controlValue, valueValue);
            return dpt2Value;
        }

        protected override bool ValueRead(object value)
        {
            if (value is Knx1BitControlled dpt2Value)
            {
                foreach (var child in _children)
                {
                    var childValue = child.Index == KnxDpt2ValueAttribute.ControlIndex ? dpt2Value.Control : dpt2Value.Value;
                    child.Value = childValue;
                    child.DispatchRead(childValue);
                }
            }

            return false; // the folder node itself has no value
        }

        internal Task WriteInternal(object value, IWriteContext writeContext,
            CancellationToken token = new CancellationToken())
        {
            return Write(value, writeContext, token);
        }

        internal Task<bool> ReadFromBus()
        {
            return Driver.Read(GroupAddress);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var att = new KnxDpt2ValueAttribute(ctx, this, ctx.NodeInstance.This2NodeTemplateNavigation.Key == "knx-dpt2-value" ? KnxDpt2ValueAttribute.ValueIndex : KnxDpt2ValueAttribute.ControlIndex);

            _children.Add(att);
            return att;
        }
    }
}
