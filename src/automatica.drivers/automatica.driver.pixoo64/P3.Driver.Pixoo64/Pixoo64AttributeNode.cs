using System;
using System.Threading;
using System.Threading.Tasks;
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

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                await _screen.SetValue(value, DriverContext.NodeInstance, token);
                await writeContext.DispatchValue(value, token);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError($"Could not set screen value {e}");
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
