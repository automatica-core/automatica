using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("P3.Driver.MachineFlags.Tests")]

namespace P3.Driver.MachineFlags.Attributes
{
    public class StringBinaryFlag : DriverBase
    {

        private string _value;
        public StringBinaryFlag(IDriverContext ctx) : base(ctx)
        {

        }

        public override Task WriteValue(IDispatchable source, object value)
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

                DispatchValue(sValue);
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
