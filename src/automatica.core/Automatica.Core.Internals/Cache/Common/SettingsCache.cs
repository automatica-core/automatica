using System.Linq;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class SettingsCache : AbstractCache<long, Setting>, ISettingsCache
    {
        public SettingsCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<Setting> GetAll(AutomaticaContext context)
        {
            var settings = context.Settings.AsNoTracking().Where(a => a.IsVisible).OrderBy(a => a.Order).ToList();

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

            return settings.OrderBy(a => a.Order).AsQueryable();
        }

        protected override long GetKey(Setting obj)
        {
            return obj.ObjId;
        }
    }
}
