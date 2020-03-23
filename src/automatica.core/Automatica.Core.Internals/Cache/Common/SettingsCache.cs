using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class SettingsCache : AbstractCache<long, Setting>, ISettingsCache
    {
        private readonly IDictionary<string, Setting> _byKeyCache = new ConcurrentDictionary<string, Setting>();

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

            foreach (var setting in settings)
            {
                _byKeyCache.Add(setting.ValueKey, setting);
            }

            var all = settings.OrderBy(a => a.Order).AsQueryable();

            return all;
        }

        protected override long GetKey(Setting obj)
        {
            return obj.ObjId;
        }

        public override void Clear()
        {
            base.Clear();
            _byKeyCache.Clear();
        }

        public Setting GetByKey(string key)
        {
            return _byKeyCache[key];
        }
    }
}
