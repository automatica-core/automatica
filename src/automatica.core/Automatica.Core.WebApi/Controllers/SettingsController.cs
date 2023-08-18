using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Core;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/settings")]
    public class SettingsController : BaseController
    {
        private readonly IAutoUpdateHandler _updateHandler;
        private readonly ISettingsCache _settingsCache;
        private readonly ICoreServer _coreServer;

        public SettingsController(AutomaticaContext dbContext, IAutoUpdateHandler updateHandler, ISettingsCache settingsCache, ICoreServer coreServer) : base(dbContext)
        {
            _updateHandler = updateHandler;
            _settingsCache = settingsCache;
            _coreServer = coreServer;
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
            var reloadServer = false;
            foreach(var s in settings)
            {
                var originalSetting = DbContext.Settings.SingleOrDefault(a => a.ValueKey == s.ValueKey);

                if(originalSetting == null)
                {
                    // Something really fucked up
                    continue;
                }

                if (s.ValueDouble != originalSetting.ValueDouble)
                {
                    reloadServer = true;
                }

                if (s.ValueInt != originalSetting.ValueInt)
                {
                    reloadServer = true;
                }
                if (s.ValueText != originalSetting.ValueText)
                {
                    reloadServer = true;
                }
                originalSetting.Value = s.Value;

                DbContext.Update(originalSetting);
            }

            if (reloadServer)
            {
                _coreServer.ReInit().ConfigureAwait(false);
            }

            DbContext.SaveChanges();
            _updateHandler.ReInitialize();
            _settingsCache.Clear();

            return LoadSettings();
        }
    }
}
