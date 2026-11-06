using System;
using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Recorder;
using Automatica.Core.Model.Models.User;
using Automatica.Core.Runtime.RemoteConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/settings")]
    public class SettingsController : BaseController
    {
        /// <summary>
        /// The settings a non-admin may read (the app header shows the project name and uses the language).
        /// All other settings (api keys, passwords, hosts, ...) are admin only. A list in code, so it also covers existing databases.
        /// </summary>
        public static readonly IReadOnlySet<string> PublicSettingKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "projectName",
            "language",
            "timezoneOffset"
        };

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
        [Authorize(Policy = Role.AdminRole)]
        public ICollection<Setting> LoadSettings()
        {
            return _settingsCache.All();
        }

        [HttpGet]
        [Route("language")]
        [AllowAnonymous]
        public Setting GetLanguageSetting()
        {
            return _settingsCache.GetByKey("language");
        }

        [HttpGet]
        [Route("key/{key}")]
        [Authorize(Policy = Role.ViewerRole)]
        public ActionResult<Setting> GetSetting(string key)
        {
            if (!PublicSettingKeys.Contains(key) && !IsAdmin)
            {
                return Forbid();
            }

            var setting = _settingsCache.GetByKey(key);
            if (setting == null)
            {
                return NotFound();
            }
            return setting;
        }

        [HttpPost]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<ICollection<Setting>> SaveSettings([FromBody]IList<Setting> settings)
        {
            var reloadServer = false;
            var reloadContext = new List<SettingReloadContext>();

            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                reloadServer = false;
                reloadContext.Clear();

                // rolled back on dispose unless committed
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
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

                        originalSetting.ModifiedAt = DateTimeOffset.Now;
                        DbContext.Update(originalSetting);
                    }

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not {nameof(SaveSettings)} {e}");
                    throw;
                }
            });

            try
            {
                // after the commit: these read the settings back through their own
                // connections, which only see committed data (and must not wait on our lock)
                _settingsCache.Clear();
                _config.Reload();
                await _updateHandler.ReInitialize().ConfigureAwait(false);

                if (reloadServer)
                {
                    // every changed subsystem, not only the first one (a server reload does not restart the remote connect)
                    if (reloadContext.Contains(SettingReloadContext.Server))
                        await _coreServer.ReInit().ConfigureAwait(false);
                    if (reloadContext.Contains(SettingReloadContext.Recorders))
                        await _recorderContext.Reload().ConfigureAwait(false);
                    if (reloadContext.Contains(SettingReloadContext.RemoteConnect))
                        await _remoteConnectService.ReloadAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                // the settings are saved, but applying them failed - report it instead of returning success
                _logger.LogError(e, $"Could not apply the saved settings in {nameof(SaveSettings)} {e}");
                throw;
            }

            return LoadSettings();
        }
    }
}
