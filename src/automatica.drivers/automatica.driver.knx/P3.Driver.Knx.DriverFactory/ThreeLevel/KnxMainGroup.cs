﻿using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using System.Threading.Tasks;
using System.Threading;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public class KnxMainGroup : KnxLevelBase
    {
        public KnxMainGroup(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        public sealed override Task WriteValue(IDispatchable source, object value)
        {
            return Task.CompletedTask;
        }

        public sealed override Task WriteValue(IDispatchable source, DispatchValue value, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        public sealed override Task<bool> Read(CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }
        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var m = new KnxMiddleGroup(ctx, Driver);

            AddChild(m);
            return m;
        }
    }
}
