using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Common;
using Automatica.Core.Internals.Core;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("settings")]
    public class SettingsController : BaseController
    {
        private readonly IAutoUpdateHandler _updateHandler;

        public SettingsController(AutomaticaContext dbContext, IAutoUpdateHandler updateHandler) : base(dbContext)
        {
            _updateHandler = updateHandler;
        }

        [HttpGet]
        public IList<Setting> LoadSettings()
        {
            var settings = DbContext.Settings.Where(a => a.IsVisible).OrderBy(a => a.Order).ToList();


            settings.Add(new Setting
            {
                ValueKey = "ServerUid",
                Order = -1,
                Type = (long)PropertyTemplateType.Text,
                Value = ServerInfo.ServerUid,
                Group = "SERVER.COMMON",
                IsReadonly = true,
                IsVisible = true,
                ObjId = -5000
            });

            return settings.OrderBy(a => a.Order).ToList();
        }

        [HttpPost]
        public IList<Setting> SaveSettings([FromBody]IList<Setting> settings)
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

            return LoadSettings();
        }
    }
}
