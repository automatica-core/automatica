using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.Templates
{
    public interface ISettingsFactory
    {
        /// <summary>
        /// Adds global settings to the database
        /// </summary>
        /// <param name="key">The unique key</param>
        /// <param name="value">Value</param>
        /// <param name="group">Group</param>
        /// <param name="type"><see cref="PropertyTemplateType"/></param>
        /// <param name="isVisible">Is visible to user</param>
        void AddSettingsEntry(string key, object value, string group, PropertyTemplateType type, bool isVisible);

        /// <summary>
        /// Returns the <see cref="Setting"/> if found
        /// </summary>
        /// <param name="key">The unique key</param>
        /// <returns><see cref="Setting"/></returns>
        Setting GetSetting(string key);
    }
}
