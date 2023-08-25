using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Sonos;

namespace P3.Driver.SonosDriverFactory.Attributes
{
    public class SonosSetRadioAttribute : SonosAttribute
    {
        protected SonosDevice Device { get; }

        private string _currentMediaUrl = String.Empty;

        public SonosSetRadioAttribute(IDriverContext driverContext, SonosDevice device) : base(driverContext, () => null, o => null)
        {
            Device = device;
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var currentMediaInfo = await Device.Controller.GetMediaInfoAsync();
                _currentMediaUrl = currentMediaInfo.CurrentUri;
                await readContext.DispatchValue(_currentMediaUrl, token);

            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Error read current media url...");
            }

            return true;
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var strValue = value.ToString();
                var mediaUrl = String.Format(SonosController.TuneInMediaUrl, strValue);

                if (mediaUrl != _currentMediaUrl)
                {
                    await Device.Controller.SetMediaUrl(mediaUrl);
                    _currentMediaUrl = mediaUrl;

                    await writeContext.DispatchValue(value, token);

                    DriverContext.Logger.LogDebug($"Sonos set radio to {mediaUrl}...");
                }
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
