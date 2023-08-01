﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Sonos;
using Timer = System.Timers.Timer;

namespace P3.Driver.SonosDriverFactory.Attributes
{
    public class SonosSetRadioAttribute : DriverBase
    {
        protected SonosDevice Device { get; }
        private readonly Timer _readTimer = new Timer();

        private string _currentMediaUrl = String.Empty;

        public SonosSetRadioAttribute(IDriverContext driverContext, SonosDevice device) : base(driverContext)
        {
            Device = device;
            _readTimer.Elapsed += ReadTimerOnElapsed;
            _readTimer.Interval = TimeSpan.FromSeconds(20).TotalMilliseconds;
        }

        private async void ReadTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var currentMediaInfo = await Device.Controller.GetMediaInfoAsync();
                _currentMediaUrl = currentMediaInfo.CurrentUri;
                
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Error read current media url...");
            }
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _readTimer.Start();
            return base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _readTimer.Elapsed += ReadTimerOnElapsed;
            _readTimer.Stop();
            return base.Stop(token);
        }

        public override async Task WriteValue(IDispatchable source, object value)
        {
            try
            {
                var strValue = value.ToString();
                var mediaUrl = String.Format(SonosController.TuneInMediaUrl, strValue);

                if (mediaUrl != _currentMediaUrl)
                {
                    await Device.Controller.SetMediaUrl(mediaUrl);
                    _currentMediaUrl = mediaUrl;

                    DispatchValue(value);

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
