using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Automatica.Core.Base.Templates;
using P3.Driver.SpotPrice.Driver;
using Xunit;

namespace P3.Driver.SpotPrice.Tests
{
    /// <summary>
    /// Every translation key of the driver exists in the English and the German resource file
    /// </summary>
    public class SpotPriceTextsTests
    {
        private static JsonElement Load(string lang, out JsonDocument doc)
        {
            var assembly = typeof(SpotPriceDriverFactory).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($"SpotPrice-{lang}.json"));
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
            doc = JsonDocument.Parse(reader.ReadToEnd());
            return doc.RootElement;
        }

        private static string Resolve(JsonElement root, string key)
        {
            var element = root;
            foreach (var part in key.Split('.'))
            {
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(part, out element))
                {
                    return null;
                }
            }
            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        }

        private static string EnumKey(Enum value)
        {
            var field = value.GetType().GetField(value.ToString())!;
            var attribute = field.GetCustomAttributesData().Single(a => a.AttributeType == typeof(EnumNameAttribute));
            return (string)attribute.ConstructorArguments[0].Value;
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void AllKeysExist(string lang)
        {
            var root = Load(lang, out var doc);
            using var _ = doc;

            var sections = new[]
                {
                    "SPOT_PRICE", "SPOT_PRICE.PROVIDER", "SPOT_PRICE.TOKEN", "SPOT_PRICE.ZONE", "SPOT_PRICE.SURCHARGE", "SPOT_PRICE.VAT",
                    "SPOT_PRICE.REFRESH", "SPOT_PRICE.API_URL", "SPOT_PRICE.WINDOW", "SPOT_PRICE.WINDOW.HOURS", "SPOT_PRICE.WINDOW.PERIOD",
                    "SPOT_PRICE.WINDOW.CONTIGUOUS", "SPOT_PRICE.WINDOW.MODE"
                }
                .Concat(SpotPriceDataPoints.All.Select(d => $"SPOT_PRICE.{d.I18N}"));
            var keys = sections.SelectMany(s => new[] { $"{s}.NAME", $"{s}.DESCRIPTION" })
                .Concat(Enum.GetValues<SpotPriceProvider>().Select(v => EnumKey(v)))
                .Concat(Enum.GetValues<SpotWindowMode>().Select(v => EnumKey(v)));

            foreach (var key in keys)
            {
                Assert.False(string.IsNullOrEmpty(Resolve(root, key)), $"{lang}: {key}");
            }
        }
    }
}
