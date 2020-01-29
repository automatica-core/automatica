using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Core.Base.Localization
{
    public class LocalizationProvider : ILocalizationProvider
    {
        private readonly Dictionary<string, JObject> _localizationStreams = new Dictionary<string, JObject>();

        private readonly List<Assembly> _loadedAssemblies = new List<Assembly>();
        private readonly ILogger _logger;

        public LocalizationProvider(ILogger logger)
        {
            _logger = logger;
        }

        public void AddLocalization(TextReader stream, CultureInfo culture)
        {
            if (!_localizationStreams.ContainsKey(culture.TwoLetterISOLanguageName))
            {
                _localizationStreams.Add(culture.TwoLetterISOLanguageName, new JObject());
            }

            var jObject = _localizationStreams[culture.TwoLetterISOLanguageName];

            
            JsonTextReader reader = new JsonTextReader(stream);

            JObject o2 = (JObject)JToken.ReadFrom(reader);

            foreach (var d in o2.Children())
            {
                if (!jObject.ContainsKey(d.Path))
                {
                    jObject.Add(d);
                }
            }

            _localizationStreams[culture.TwoLetterISOLanguageName] = jObject;
        }

        public void LoadFromAssembly(Assembly assembly)
        {
            if(_loadedAssemblies.Contains(assembly))
            {
                return;
            }

            foreach(var file in assembly.GetManifestResourceNames())
            {
                if (file.EndsWith(".json") && !file.EndsWith("automatica-manifest.json"))
                {
                    try
                    {
                        var fileName = file.Replace(".json", "");
                        var split = fileName.Split("-");

                        var culture = CultureInfo.GetCultureInfo(split[split.Length - 1]);
                        AddLocalization(new StreamReader(assembly.GetManifestResourceStream(file)), culture);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Could not load localization file ({file}) from assembly {assembly}");
                    }
                }
            }
            _loadedAssemblies.Add(assembly);
        }

        public object ToJson(string locale)
        {
            var json = new object();
            if (!_localizationStreams.ContainsKey(locale))
            {
                return null;
            }
            return JsonConvert.SerializeObject(_localizationStreams[locale]);
        }
    }
}
