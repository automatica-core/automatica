using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Configuration;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Recorder;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/settings")]
    public class SettingsController : BaseController
    {
        private readonly IAutoUpdateHandler _updateHandler;
        private readonly ISettingsCache _settingsCache;
        private readonly ICoreServer _coreServer;
        private readonly IRecorderContext _recorderContext;
        private readonly IConfiguration _config;

        public SettingsController(AutomaticaContext dbContext, 
            IAutoUpdateHandler updateHandler, 
            ISettingsCache settingsCache, 
            ICoreServer coreServer, 
            IRecorderContext recorderContext,
            IConfiguration config) : base(dbContext)
        {
            _updateHandler = updateHandler;
            _settingsCache = settingsCache;
            _coreServer = coreServer;
            _recorderContext = recorderContext;
            _config = config;
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
        public ICollection<Setting> SaveSettings([FromBody]IList<Setting> settings)
        {
            using var context = new AutomaticaContext(_config);
            var reloadServer = false;
            var reloadContext = new List<SettingReloadContext>();
            foreach(var s in settings)
            {
                var originalSetting = DbContext.Settings.SingleOrDefault(a => a.ValueKey == s.ValueKey);

                if(originalSetting == null)
                {
                    // Something really fucked up
                    continue;
                }

                if (s.ValueDouble != originalSetting.ValueDouble && originalSetting.NeedsReloadOnChange)
                {
                    reloadServer = true;
                    if(!reloadContext.Contains(originalSetting.ReloadContext))
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

            context.SaveChanges();
            _updateHandler.ReInitialize().ConfigureAwait(false);
            _settingsCache.Clear();

            if (reloadServer)
            {
                if(reloadContext.Contains(SettingReloadContext.Server))
                    _coreServer.ReInit().ConfigureAwait(false);
                else if(reloadContext.Contains(SettingReloadContext.Recorders))
                    _recorderContext.Reload().ConfigureAwait(false);
            }

           
            return LoadSettings();
        }
    }
}
