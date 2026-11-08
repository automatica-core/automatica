using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Energy.EnergyManager;
using P3.Logic.Operations.Energy.LoadManager;
using Xunit;

namespace P3.Logic.Operations.Tests.Energy
{
    /// <summary>
    /// Every translation key of the energy logics exists in the English and the German resource file
    /// </summary>
    public class EnergyTextsTests
    {
        public static IEnumerable<object[]> Cases()
        {
            foreach (var lang in new[] { "en", "de" })
            {
                yield return new object[] { lang, typeof(EnergyManagerLogicFactory).FullName };
                yield return new object[] { lang, typeof(LoadManagerLogicFactory).FullName };
            }
        }

        private static JsonDocument Load(string lang)
        {
            var assembly = typeof(EnergyManagerLogicFactory).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($"Operations-{lang}.json"));
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return JsonDocument.Parse(reader.ReadToEnd());
        }

        private static bool Exists(JsonDocument doc, string key)
        {
            var element = doc.RootElement;
            foreach (var part in key.Split('.'))
            {
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(part, out element))
                {
                    return false;
                }
            }
            return element.ValueKind == JsonValueKind.String && element.GetString()!.Length > 0;
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public void AllKeysExist(string lang, string factoryType)
        {
            var factory = (LogicFactory)System.Activator.CreateInstance(typeof(EnergyManagerLogicFactory).Assembly.GetType(factoryType)!)!;
            var templates = new LogicTemplateFactoryMock();
            factory.InitTemplates(templates);
            var rule = templates.CreateRuleInstanceFromTemplate(factory.LogicGuid);

            var keys = new List<string>
            {
                rule.This2RuleTemplateNavigation.Name,
                rule.This2RuleTemplateNavigation.Description,
                rule.This2RuleTemplateNavigation.Group
            };
            foreach (var item in rule.RuleInterfaceInstance.Select(i => i.This2RuleInterfaceTemplateNavigation))
            {
                // short pin names like "Q1" are not translated
                if (item.Name.Contains('.'))
                {
                    keys.Add(item.Name);
                }
                keys.Add(item.Description);
            }
            keys.Add("OPERATIONS.ENERGY_MANAGER.LOAD.TYPE.DIGITAL");
            keys.Add("OPERATIONS.ENERGY_MANAGER.LOAD.TYPE.ANALOG");

            using var doc = Load(lang);
            var missing = keys.Distinct().Where(k => !Exists(doc, k)).ToList();
            Assert.Empty(missing);
        }
    }
}
