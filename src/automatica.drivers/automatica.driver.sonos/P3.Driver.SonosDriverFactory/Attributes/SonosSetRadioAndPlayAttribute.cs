using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.SonosDriverFactory.Attributes
{
    public class SonosSetRadioAndPlayAttribute : SonosSetRadioAttribute
    {

        public SonosSetRadioAndPlayAttribute(IDriverContext driverContext, SonosDevice device) : base(driverContext, device)
        {
        }

        public override async Task WriteValue(IDispatchable source, object value)
        {
            try
            {
                await base.WriteValue(source, value);
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
