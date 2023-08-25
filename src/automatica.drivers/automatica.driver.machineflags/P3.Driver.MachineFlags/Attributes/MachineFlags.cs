using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
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

        public override Task WriteValue(IDispatchable source, object value)
        {
            if (_value == value)
            {
                return Task.CompletedTask;
            }
            _value = value;
            DriverContext.Logger.LogDebug($"WriteValue {value}");

            DispatchRead(value);
            return Task.CompletedTask;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new InvalidOperationException();
        }
    }
}
