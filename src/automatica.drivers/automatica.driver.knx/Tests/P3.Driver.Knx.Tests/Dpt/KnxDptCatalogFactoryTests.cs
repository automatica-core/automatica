using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Automatica.Core.Base.Templates;
using Automatica.Core.UnitTests.Base.Drivers;
using Newtonsoft.Json.Linq;
using P3.Driver.Knx.DriverFactory.Dpt;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using Xunit;

namespace P3.Driver.Knx.Tests.Dpt
{
    /// <summary>
    /// The catalogue datapoint types are selectable like the existing ones: node templates with address, DPT and sub-type properties and translations.
    /// </summary>
    public class KnxDptCatalogFactoryTests : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        [Fact]
        public void EveryCatalogEntryHasNodeTemplateWithProperties()
        {
            foreach (var entry in KnxDptCatalog.Entries)
            {
                var node = CreateNodeInstance(entry.TemplateGuid);
                Assert.True(node != null, entry.Key);
                Assert.Equal(entry.Key, node.This2NodeTemplateNavigation.Key);
                Assert.NotNull(node.GetProperty("knx-address"));
                Assert.NotNull(node.GetProperty("knx-dpt"));

                if (entry.SubTypeEnum != null)
                {
                    Assert.True(node.GetProperty("knx-dpt-sub") != null, entry.Key);
                }

                foreach (var child in entry.Children)
                {
                    var childNode = CreateNodeInstance(child.Guid);
                    Assert.True(childNode != null, child.TemplateKey(entry));
                    Assert.Equal(child.TemplateKey(entry), childNode.This2NodeTemplateNavigation.Key);
                }
            }
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void EveryCatalogKeyIsTranslated(string language)
        {
            var json = LoadResource(language);
            var missing = KnxDptCatalog.Entries
                .SelectMany(e => new[] { e.NameKey }
                    .Concat(e.Children.Select(c => c.NameKey(e)))
                    .Concat(e.SubTypeEnum == null
                        ? Enumerable.Empty<string>()
                        : e.SubTypeEnum.GetFields(BindingFlags.Public | BindingFlags.Static)
                            .Select(f => f.GetCustomAttribute<EnumNameAttribute>()?.TranslationKey)
                            .Where(a => a != null)))
                .Where(key => json.SelectToken(key) == null)
                .ToList();

            Assert.True(missing.Count == 0, $"{language}: {string.Join(", ", missing)}");
        }

        private static JObject LoadResource(string language)
        {
            var assembly = typeof(KnxDptCatalog).Assembly;
            var name = assembly.GetManifestResourceNames().Single(a => a.EndsWith($"Knx-{language}.json", StringComparison.Ordinal));
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return JObject.Parse(reader.ReadToEnd());
        }
    }
}
