using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Knx.Falcon.ApplicationData;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    /// <summary>
    /// Child of a DPT 3 node: the control bit (direction) or the step code (0 = break, 1-7 = interval).
    /// </summary>
    public class KnxDpt3ValueAttribute : DriverBase
    {
        internal const string ControlKey = "knx-dpt3-control";
        internal const string StepCodeKey = "knx-dpt3-step-code";

        private readonly KnxDpt3Attribute _parent;

        public bool IsControl { get; }

        /// <summary>
        /// bool for the control bit, int for the step code
        /// </summary>
        public object Value { get; set; }

        public KnxDpt3ValueAttribute(IDriverContext driverContext, KnxDpt3Attribute parent, bool isControl) : base(driverContext)
        {
            _parent = parent;
            IsControl = isControl;
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            object newValue;
            if (IsControl)
            {
                newValue = Convert.ToBoolean(value);
            }
            else
            {
                var stepCode = Convert.ToInt32(value);
                if (stepCode is < 0 or > 7)
                {
                    DriverContext.Logger.LogWarning($"{_parent.GroupAddress}: step code {value} is out of range (0..7), ignore write");
                    return;
                }
                newValue = stepCode;
            }

            if (Equals(newValue, Value))
            {
                await writeContext.DispatchValue(newValue, token);
                return;
            }

            // the parent encodes the state of all children, so the new value must be set before writing
            var oldValue = Value;
            Value = newValue;

            try
            {
                await _parent.WriteInternal(newValue, writeContext, token); // dispatches the value on success
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

    public class KnxDpt3Attribute : KnxGroupAddress
    {
        private readonly List<KnxDpt3ValueAttribute> _children = new List<KnxDpt3ValueAttribute>();

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt3;
        public KnxDpt3Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override object ConvertToDptValue(object value)
        {
            var control = _children.FirstOrDefault(a => a.IsControl)?.Value as bool? ?? false;
            var stepCode = _children.FirstOrDefault(a => !a.IsControl)?.Value as int? ?? 0;

            return new Knx3BitControlled(control, (byte)stepCode);
        }

        protected override bool ValueRead(object value)
        {
            if (value is Knx3BitControlled dpt3Value)
            {
                foreach (var child in _children)
                {
                    object childValue = child.IsControl ? dpt3Value.Control : (int)dpt3Value.Value;
                    child.Value = childValue;
                    child.DispatchRead(childValue);
                }
            }

            return false; // the folder node itself has no value
        }

        internal Task WriteInternal(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
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
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;
            if (key != KnxDpt3ValueAttribute.ControlKey && key != KnxDpt3ValueAttribute.StepCodeKey)
            {
                return null;
            }

            var att = new KnxDpt3ValueAttribute(ctx, this, key == KnxDpt3ValueAttribute.ControlKey);
            _children.Add(att);
            return att;
        }
    }
}
