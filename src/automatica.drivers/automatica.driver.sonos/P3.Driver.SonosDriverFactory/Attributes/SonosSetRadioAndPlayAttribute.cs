using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.SonosDriverFactory.Attributes
{
    public class SonosSetRadioAndPlayAttribute : SonosSetRadioAttribute
    {

        public SonosSetRadioAndPlayAttribute(IDriverContext driverContext, SonosDevice device) : base(driverContext, device)
        {
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                DriverContext.Logger.LogDebug($"Sonos play...");
                await base.Write(value, writeContext, token);
                await Device.Controller.PlayAsync();
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Could not set radio...");
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
