using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Pixoo64
{
    internal class Pixoo64AttributeNode : DriverBase
    {
        private readonly Pixoo64Screen _screen;

        public Pixoo64AttributeNode(IDriverContext driverContext, Pixoo64Screen screen) : base(driverContext)
        {
            _screen = screen;
        }

        public override async Task WriteValue(IDispatchable source, DispatchValue value, CancellationToken token = new CancellationToken())
        {
            try
            {
                await _screen.SetValue(value.Value, DriverContext.NodeInstance, token);
                DispatchValue(value.Value);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError($"Could not set screen value {e}");
            }
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
