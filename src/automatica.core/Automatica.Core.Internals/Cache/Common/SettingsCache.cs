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
        // replaced as a whole when the cache is (re)filled, readers never see a half filled or cleared dictionary
        private volatile ConcurrentDictionary<string, Setting> _byKeyCache = new ConcurrentDictionary<string, Setting>();

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

            var byKey = new ConcurrentDictionary<string, Setting>();
            foreach (var setting in settings)
            {
                byKey[setting.ValueKey] = setting;
            }
            _byKeyCache = byKey;

            var all = settings.OrderBy(a => a.Order).AsQueryable();

            return all;
        }

        protected override long GetKey(Setting obj)
        {
            return obj.ObjId;
        }

        public override void Clear()
        {
            // the by-key dictionary is replaced on the next access (Initialize runs GetAll again), until then the old values are returned
            base.Clear();
        }

        public void UpdateByKey(string key, Setting value)
        {
            Initialize();
            _byKeyCache[key] = value;
        }

        /// <returns>the setting, null if there is no (visible) setting with this key</returns>
        public Setting GetByKey(string key)
        {
            Initialize();
            return key != null && _byKeyCache.TryGetValue(key, out var setting) ? setting : null;
        }
    }
}
