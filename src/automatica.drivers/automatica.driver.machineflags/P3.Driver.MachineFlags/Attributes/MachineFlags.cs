using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("P3.Driver.MachineFlags.Tests")]

namespace P3.Driver.MachineFlags.Attributes
{
    public class MachineFlags : DriverNoneAttributeBase
    {

        private object _value;
        public MachineFlags(IDriverContext ctx) : base(ctx)
        {

        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await readContext.DispatchValue(_value, token);
            return true;
        }
        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            if (_value == value)
            {
                return Task.CompletedTask;
            }
            _value = value;
            DriverContext.Logger.LogDebug($"{DriverContext.NodeInstance.ObjId} {DriverContext.NodeInstance.Name}: WriteValue {value}");

            writeContext.DispatchValue(value, token);
            return Task.CompletedTask;
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new InvalidOperationException();
        }
    }
}
