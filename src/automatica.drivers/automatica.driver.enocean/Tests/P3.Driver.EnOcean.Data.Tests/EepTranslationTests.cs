using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.EnOcean.DriverFactory;
using Xunit;

namespace P3.Driver.EnOcean.Data.Tests
{
    /// <summary>
    /// Every node template the driver creates has its name and description in every locale.
    /// </summary>
    public class EepTranslationTests : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private static Dictionary<string, string> Load(string locale)
        {
            var assembly = typeof(EnOceanDriverFactory).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($"EnOcean-{locale}.json"));
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var document = JsonDocument.Parse(stream);

            var result = new Dictionary<string, string>();
            void Flatten(JsonElement element, string prefix)
            {
                foreach (var property in element.EnumerateObject())
                {
                    var key = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        Flatten(property.Value, key);
                    }
                    else
                    {
                        result[key] = property.Value.GetString();
                    }
                }
            }

            Flatten(document.RootElement, "");
            return result;
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void EveryEepTemplate_HasANameAndADescription(string locale)
        {
            var translations = Load(locale);
            var rorgClasses = new[] { typeof(EnOceanRorgF6Data), typeof(EnOceanRorgD5Data), typeof(EnOceanRorgA5Data), typeof(EnOceanRorgD2Data) };

            var missing = new List<string>();
            var checkedTemplates = 0;
            foreach (var field in rorgClasses.SelectMany(c => c.GetFields(BindingFlags.Public | BindingFlags.Static)).Where(f => f.FieldType == typeof(Guid)))
            {
                var template = Factory.GetNodeTemplateById((Guid)field.GetValue(null)!);
                checkedTemplates++;
                foreach (var key in new[] { template.Name, template.Description })
                {
                    if (!translations.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text) && !key.Contains("SHORTCUT_."))
                    {
                        missing.Add(key);
                    }
                }
            }

            Assert.True(checkedTemplates > 6000, $"only {checkedTemplates} templates checked");
            Assert.True(missing.Count == 0, $"{missing.Count} keys missing in EnOcean-{locale}.json, e.g.{Environment.NewLine}{string.Join(Environment.NewLine, missing.Take(20))}");
        }
    }
}
