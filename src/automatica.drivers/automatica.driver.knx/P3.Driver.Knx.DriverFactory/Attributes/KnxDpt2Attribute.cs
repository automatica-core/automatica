using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.DPT;
using P3.Knx.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt2ValueAttribute : DriverBase
    {
        private readonly KnxDpt2Attribute _parent;

        public int Index { get; }
        public bool? Value { get; set; }

        public KnxDpt2ValueAttribute(IDriverContext driverContext, KnxDpt2Attribute parent, int index) : base(driverContext)
        {
            _parent = parent;
            Index = index;
        }

        public void DispatchDpt2Value(Dpt2Value value)
        {
            if (Index == 0)
            {
                DispatchValue(value.Value);
                Value = value.Value;
            }
            DispatchValue(value.Control);
            Value = value.Control;
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var boolValue = Convert.ToBoolean(value);

            if (boolValue != Value)
            {
                _parent.WriteToBus();
            }

            Value = boolValue;

            return Task.CompletedTask;
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

        protected override string GetDptString(int dpt)
        {
            return PropertyHelper.GetNameAttributeFromEnumValue((DptType)dpt).EnumValue;
        }

        internal void WriteToBus()
        {
            var controlAttribute = _children.SingleOrDefault(a => a.Index == 1);
            var valueAttribute = _children.SingleOrDefault(a => a.Index == 0);

            var controlValue = controlAttribute?.Value ?? false;
            var valueValue = valueAttribute?.Value ?? false;

            var dpt2Value = new Dpt2Value(controlValue, valueValue);

            Tunneling.Write(GroupAddress, ConvertToBus(dpt2Value));
        }

        protected override void ConvertFromBus(KnxDatagram datagram)
        {
            var value = DptTranslator.Instance.FromDataPoint(DptTypeString, datagram.Data);

            if (value is Dpt2Value dpt2Value)
            {
                foreach (var child in _children)
                {
                    child.DispatchDpt2Value(dpt2Value);
                }
            }
            else
            {
                KnxHelper.Logger.LogError("Received value has invalud type");
            }

            DispatchValue(value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var att = new KnxDpt2ValueAttribute(ctx, this, ctx.NodeInstance.This2NodeTemplateNavigation.Key == "knx-dpt2-value" ? 0 : 1);

            _children.Add(att);
            return att;
        }
    }
}
