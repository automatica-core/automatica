using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Media;
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
                var media = ToMedia(DriverContext.RadioStations, value?.ToString());
                if (media == null)
                {
                    DriverContext.Logger.LogWarning($"{Name}: unknown radio station \"{value}\"");
                    return;
                }

                await Device.Controller.SetRadioAsync(media);
                _currentMediaUrl = media.Uri;

                await writeContext.DispatchValue(value, token);

                DriverContext.Logger.LogDebug($"{Name}: Sonos set radio to {media.Uri}...");
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"{Name}: Could not set radio...");
            }
        }

        /// <summary>
        /// The value is a station of the core's catalog (key or name, e.g. "at-oe3" from Sonos Control), or as before the
        /// catalog a TuneIn id ("s8007") or a stream url. Null if it is none of them.
        /// </summary>
        public static SonosRadioMedia ToMedia(IRadioStationCatalog catalog, string value)
        {
            var known = (catalog ?? RadioStationCatalog.BuiltIn).Find(value);
            if (known != null)
            {
                return SonosRadio.ForStation(known.TuneInId, known.StreamUrl, known.Name);
            }

            var station = RadioStationCatalog.BuiltIn.Resolve(value);
            if (station == null)
            {
                return null;
            }

            return station.TuneInId != null
                ? SonosRadio.ForStation(station.TuneInId, null, null)
                : SonosRadio.ForStation(null, station.StreamUrl, null);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
