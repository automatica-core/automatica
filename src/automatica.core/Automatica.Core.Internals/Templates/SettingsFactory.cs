using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Templates
{
    /// <summary>
    /// Works on the given <see cref="AutomaticaContext"/>. Disposing the factory disposes that context, so only dispose
    /// factories that own their context (e.g. the ones created by a TemplateFactoryProvider).
    /// </summary>
    public class SettingsFactory : ISettingsFactory, IDisposable
    {
        private readonly IConfiguration _config;
        protected AutomaticaContext Db { get; }
        
        public SettingsFactory(AutomaticaContext database, IConfiguration config)
        {
            Db = database;
            _config = config;
        }
        public virtual async Task CommitChanges()
        {
            await Db.SaveChangesAsync();
        }
        public void AddSettingsEntry(string key, object value, string group, PropertyTemplateType type, bool isVisible)
        {
            var settings = Db.Settings.SingleOrDefault(a => a.ValueKey == key);
            var isNewObject = false;
            if (settings == null)
            {
                settings = new Setting
                {
                    ValueKey = key,
                    Value = value
                };
                isNewObject = true;
            }
            settings.Type = (long)type;
            settings.IsVisible = isVisible;
            settings.Group = group;

            if (isNewObject)
            {
                Db.Settings.Add(settings);
            }
            else
            {
                Db.Settings.Update(settings);
            }
            Db.SaveChanges();
        }

        public Setting GetSetting(string key)
        {
            using var db = new AutomaticaContext(_config);
            return db.Settings.SingleOrDefault(a => a.ValueKey == key);
        }

        public void Dispose()
        {
            Db.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
