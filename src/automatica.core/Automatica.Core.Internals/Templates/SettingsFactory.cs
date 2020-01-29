using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Templates
{
    public class SettingsFactory : ISettingsFactory
    {
        private readonly IConfiguration _config;
        protected AutomaticaContext Db { get; }
        
        public SettingsFactory(AutomaticaContext database, IConfiguration config)
        {
            Db = database;
            _config = config;
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
            using (var db = new AutomaticaContext(_config))
            {
                return db.Settings.SingleOrDefault(a => a.ValueKey == key);
            }
        }
    }
}
