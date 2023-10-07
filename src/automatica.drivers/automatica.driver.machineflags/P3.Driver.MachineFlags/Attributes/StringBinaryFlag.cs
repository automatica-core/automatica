using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("P3.Driver.MachineFlags.Tests")]

namespace P3.Driver.MachineFlags.Attributes
{
    public class StringBinaryFlag : DriverNoneAttributeBase
    {

        private string _value;
        public StringBinaryFlag(IDriverContext ctx) : base(ctx)
        {

        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var sValue = value.ToString();

                if (_value == sValue)
                {
                    return Task.CompletedTask;
                }

                _value = sValue;
                DriverContext.Logger.LogDebug($"WriteValue {sValue}");

                writeContext.DispatchValue(sValue, token);
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, $"Could not convert value to bool {ex}");
            }


            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new InvalidOperationException();
        }
    }
}
