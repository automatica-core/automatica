using System.Reflection;

namespace Automatica.Core.Base.Localization
{
    /// <summary>
    /// Interface to provide localization data
    /// </summary>
    public interface ILocalizationProvider
    {
        /// <summary>
        /// Loads json locale files
        /// </summary>
        /// <param name="assembly"></param>
        void LoadFromAssembly(Assembly assembly);

        string GetTranslation(string locale, string key);

        /// <summary>
        /// Returns all loaded localization data as json
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        object ToJson(string locale);
    }
}
