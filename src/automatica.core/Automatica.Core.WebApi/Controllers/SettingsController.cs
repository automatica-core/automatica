using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Core;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("settings")]
    public class SettingsController : BaseController
    {
        private readonly IAutoUpdateHandler _updateHandler;
        private readonly ISettingsCache _settingsCache;

        public SettingsController(AutomaticaContext dbContext, IAutoUpdateHandler updateHandler, ISettingsCache settingsCache) : base(dbContext)
        {
            _updateHandler = updateHandler;
            this._settingsCache = settingsCache;
        }

        [HttpGet]
        public ICollection<Setting> LoadSettings()
        {
            return _settingsCache.All();
        }

        [HttpPost]
        public ICollection<Setting> SaveSettings([FromBody]IList<Setting> settings)
        {
            foreach(var s in settings)
            {
                var originalSetting = DbContext.Settings.SingleOrDefault(a => a.ValueKey == s.ValueKey);

                if(originalSetting == null)
                {
                    // Something really fucked up
                    continue;
                }

                originalSetting.Value = s.Value;

                DbContext.Update(originalSetting);
            }


            DbContext.SaveChanges();
            _updateHandler.ReInitialize();
            _settingsCache.Clear();

            return LoadSettings();
        }
    }
}
