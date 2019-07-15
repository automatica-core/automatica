using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Automatica.Core.Base.Localization
{
    /// <summary>
    /// Interface to provide locatization data
    /// </summary>
    public interface ILocalizationProvider
    {
        void LoadFromAssembly(Assembly assembly);
        void AppendDictionary(Dictionary<string, JObject> data);

        object ToJson(string locale);
        string ToJson();
    }
}
