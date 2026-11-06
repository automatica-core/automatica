using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;
using Automatica.Core.Runtime.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core.Update
{
    public class NativeUpdateHandler : IUpdateHandler
    {
        private readonly IConfiguration _config;
        private readonly ILogger<NativeUpdateHandler> _logger;
        private readonly ICloudApi _api;

        private readonly Timer _timer = new Timer();


        public NativeUpdateHandler(IConfiguration config, ILogger<NativeUpdateHandler> logger, ICloudApi api)
        {
            _config = config;
            _logger = logger;
            _api = api;

            _timer.Elapsed += _timer_Elapsed;
        }

        private async void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // async void timer callback: an unhandled exception here would crash the process
            try
            {
                _logger.LogInformation("Check for update");
                _timer.Stop();
                var update = await _api.CheckForUpdates();

                if (update != null)
                {
                    _logger.LogInformation("Download update");
                    var fileInfo = await _api.DownloadUpdate(update);

                    var check = Common.Update.Update.CheckUpdateFile(_logger, fileInfo.FullName, ServerInfo.Rid);

                    if (!check)
                    {
                        _logger.LogError("The downloaded update file is invalid, update is not installed");
                        _api.DeleteUpdate();
                    }
                    else
                    {
                        _logger.LogInformation("Install update");
                        await Update();
                    }
                }
            }
            catch (Exception ex)
            {
                // offline, no api key, missing settings, ...
                _logger.LogError(ex, $"Could not check for or install updates {ex}");
            }

            try
            {
                await Init();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not schedule the next update check {ex}");
            }
        }

        public Task Update()
        {
            Environment.Exit(ServerInfo.ExitCodeUpdateInstall);
            return Task.CompletedTask;
        }

        public async Task<IServerVersion> CheckForUpdates()
        {
            return await _api.CheckForUpdates();
        }

        public Task<bool> UpdateAlreadyDownloaded()
        {
            return _api.UpdateAlreadyDownloaded();
        }

        public async Task<bool> DownloadUpdate(IServerVersion version)
        {
            var fileInfo =  await _api.DownloadUpdate(version);
            var check = Common.Update.Update.CheckUpdateFile(_logger, fileInfo.FullName, ServerInfo.Rid);

            if (!check)
            {
                _api.DeleteUpdate();
            }

            return check;
        }


        public Task Init()
        {
            _timer.Stop();
            using (var context = new AutomaticaContext(_config))
            {
                var autoUpdate = context.Settings.SingleOrDefault(a => a.ValueKey == "autoUpdate");
                var autoUpdateTime = context.Settings.SingleOrDefault(a => a.ValueKey == "autoUpdateTime");

                if (autoUpdate != null && (bool) autoUpdate.Value && autoUpdateTime != null && autoUpdateTime.Value is DateTime updateTime)
                {
                    var now = DateTimeHelper.ProviderInstance.GetLocalNow().DateTime;
                    var updateTimeToday = new DateTime(now.Year, now.Month, now.Day, updateTime.Hour, updateTime.Minute,
                        updateTime.Second);

                    if (now > updateTimeToday)
                    {
                        updateTimeToday = updateTimeToday.AddDays(1);
                    }
                    var diff = (updateTimeToday - now).TotalSeconds;

                    _timer.Interval = diff * 1000;
                    _logger.LogInformation($"Next check for update is in {diff} seconds at {updateTimeToday}");
                    _timer.Start();
                }
                else
                {
                    _logger.LogInformation("Auto update seems to be disabled");
                }
            }
            return Task.CompletedTask;
        }

        public Task ReInitialize()
        {
            return Init();
        }
    }
}
