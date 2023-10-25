using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("P3.Driver.MachineFlags.Tests")]

namespace P3.Driver.MachineFlags.Attributes
{
    public class DoubleBinaryFlag : DriverNoneAttributeBase
    {

        private double? _value;
        public DoubleBinaryFlag(IDriverContext ctx) : base(ctx)
        {

        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await readContext.DispatchValue(_value, token);
            return true;
        }
        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var dValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_value == dValue)
                {
                    return Task.CompletedTask;
                }
                _value = dValue;

                DriverContext.Logger.LogDebug($"{DriverContext.NodeInstance.ObjId} {DriverContext.NodeInstance.Name}: WriteValue {bValue}");

                writeContext.DispatchValue(dValue, token);

                DriverContext.Dispatcher.DispatchValue(this, value);
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, $"Could not convert value to bool {ex}");
            }


            return Task.CompletedTask;
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new InvalidOperationException();
        }
    }
}
