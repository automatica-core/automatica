using System;
using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Recorder;
using Automatica.Core.Runtime.RemoteConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/settings")]
    public class SettingsController : BaseController
    {
        private readonly IAutoUpdateHandler _updateHandler;
        private readonly ISettingsCache _settingsCache;
        private readonly ICoreServer _coreServer;
        private readonly IRecorderContext _recorderContext;
        private readonly IConfigurationRoot _config;
        private readonly IRemoteConnectService _remoteConnectService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(AutomaticaContext dbContext, 
            IAutoUpdateHandler updateHandler, 
            ISettingsCache settingsCache, 
            ICoreServer coreServer, 
            IRecorderContext recorderContext,
            IConfigurationRoot config, 
            IRemoteConnectService remoteConnectService,
            ILogger<SettingsController> logger) : base(dbContext)
        {
            _updateHandler = updateHandler;
            _settingsCache = settingsCache;
            _coreServer = coreServer;
            _recorderContext = recorderContext;
            _config = config;
            _remoteConnectService = remoteConnectService;
            _logger = logger;
        }

        [HttpGet]
        public ICollection<Setting> LoadSettings()
        {
            return _settingsCache.All();
        }

        [HttpGet]
        [Route("key/{key}")]
        public Setting GetSetting(string key)
        {
            return _settingsCache.GetByKey(key);
        }

        [HttpPost]
        public async Task<ICollection<Setting>> SaveSettings([FromBody]IList<Setting> settings)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                await using var context = new AutomaticaContext(_config);
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    var reloadServer = false;
                    var reloadContext = new List<SettingReloadContext>();
                    foreach (var s in settings)
                    {
                        var originalSetting = DbContext.Settings.SingleOrDefault(a => a.ValueKey == s.ValueKey);

                        if (originalSetting == null)
                        {
                            // Something really fucked up
                            continue;
                        }

                        if (s.ValueDouble != originalSetting.ValueDouble && originalSetting.NeedsReloadOnChange)
                        {
                            reloadServer = true;
                            if (!reloadContext.Contains(originalSetting.ReloadContext))
                                reloadContext.Add(originalSetting.ReloadContext);
                        }

                        if (s.ValueInt != originalSetting.ValueInt && originalSetting.NeedsReloadOnChange)
                        {
                            reloadServer = true;
                            if (!reloadContext.Contains(originalSetting.ReloadContext))
                                reloadContext.Add(originalSetting.ReloadContext);
                        }

                        if (s.ValueText != originalSetting.ValueText && originalSetting.NeedsReloadOnChange)
                        {
                            reloadServer = true;
                            if (!reloadContext.Contains(originalSetting.ReloadContext))
                                reloadContext.Add(originalSetting.ReloadContext);
                        }

                        originalSetting.Value = s.Value;

                        context.Update(originalSetting);
                    }

                    await context.SaveChangesAsync();
                    await _updateHandler.ReInitialize().ConfigureAwait(false);
                    _settingsCache.Clear();
                    _config.Reload();

                    if (reloadServer)
                    {
                        if (reloadContext.Contains(SettingReloadContext.Server))
                            await _coreServer.ReInit().ConfigureAwait(false);
                        else if (reloadContext.Contains(SettingReloadContext.Recorders))
                            await _recorderContext.Reload().ConfigureAwait(false);
                        else if (reloadContext.Contains(SettingReloadContext.RemoteConnect))
                            await _remoteConnectService.ReloadAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(e, $"Could not {nameof(SaveSettings)} {e}");
                }

                return LoadSettings();
            });
        }
    }
}
