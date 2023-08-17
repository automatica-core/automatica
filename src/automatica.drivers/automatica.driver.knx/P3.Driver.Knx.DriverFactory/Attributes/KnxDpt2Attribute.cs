﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Knx.Falcon.ApplicationData;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

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

        public override Task WriteValue(IDispatchable source, object value)
        {
            var boolValue = Convert.ToBoolean(value);

            if (boolValue != Value)
            {
                _parent.WriteValue(source, value);
            }

            DispatchValue(boolValue);
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
        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt2;

        protected override object ConvertToDptValue(object value)
        {
            var controlAttribute = _children.SingleOrDefault(a => a.Index == 1);
            var valueAttribute = _children.SingleOrDefault(a => a.Index == 0);

            var controlValue = controlAttribute?.Value ?? false;
            var valueValue = valueAttribute?.Value ?? false;
            
            var dpt2Value = new Knx1BitControlled(controlValue, valueValue);
            return dpt2Value;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var att = new KnxDpt2ValueAttribute(ctx, this, ctx.NodeInstance.This2NodeTemplateNavigation.Key == "knx-dpt2-value" ? 0 : 1);

            _children.Add(att);
            return att;
        }
    }
}
