using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
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

        private readonly string _locale;

        public LocalizationProvider(IConfiguration config, ILogger<LocalizationProvider> logger)
        {
            _logger = logger;

            _locale = config["db:language"];
        }

        public void AddLocalization(TextReader stream, CultureInfo culture, string fileName)
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
            _logger.LogDebug($"Add localization for {culture.TwoLetterISOLanguageName} and {fileName}");
        }

        public void LoadFromAssembly(Assembly assembly)
        {
            _logger.LogDebug($"Load localization for {assembly.FullName}");
            if (_loadedAssemblies.Contains(assembly))
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
                        AddLocalization(new StreamReader(assembly.GetManifestResourceStream(file)), culture, file);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Could not load localization file ({file}) from assembly {assembly}");
                    }
                }
            }
            _loadedAssemblies.Add(assembly);
        }

        public string GetTranslation(string locale, string key)
        {
            if (string.IsNullOrWhiteSpace(locale))
            {
                return key;
            }

            if (!_localizationStreams.ContainsKey(locale))
            {
                return key;
            }

            var localeObject = _localizationStreams[locale];
            var keyElements = key.Split(".");

            if (keyElements.Length == 0)
            {
                return key;
            }

            
            var localePart = localeObject;

            for (int i = 0; i < keyElements.Length; i++)
            {
                if (!localePart.ContainsKey(keyElements[i]))
                {
                    return key;
                }

                var token = localePart[keyElements[i]];

                if (token is JObject jobj)
                {
                    localePart = jobj;
                }
                else
                {
                    if (i + 1 == keyElements.Length)
                    {
                        return token.ToString();
                    }
                    return key;
                }

                

            }

            return localePart.Value<string>();
        }

        public string GetTranslation(string key)
        {
            return GetTranslation(_locale, key);
        }

        public string GetLocale()
        {
            return _locale;
        }

        public object ToJson(string locale)
        {
            var json = new object();
            if (!_localizationStreams.ContainsKey(locale))
            {
                return JsonConvert.SerializeObject(new List<object>());
            }
            return JsonConvert.SerializeObject(_localizationStreams[locale]);
        }
    }
}
