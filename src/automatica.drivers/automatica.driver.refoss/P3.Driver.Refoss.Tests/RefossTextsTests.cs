using System.IO;
using System.Linq;
using System.Text.Json;
using P3.Driver.Refoss.Driver;
using Xunit;

namespace P3.Driver.Refoss.Tests
{
    /// <summary>
    /// Every translation key of the driver exists in the English and the German resource file
    /// </summary>
    public class RefossTextsTests
    {
        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void AllKeysExist(string lang)
        {
            var assembly = typeof(RefossDriverFactory).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($"Refoss-{lang}.json"));
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
            using var doc = JsonDocument.Parse(reader.ReadToEnd());
            var root = doc.RootElement.GetProperty("REFOSS");

            var sections = new[] { "HOST", "PASSWORD", "POLL", "SCAN" }.Concat(RefossDataPoints.Device.Select(d => d.I18N));
            Assert.False(string.IsNullOrEmpty(root.GetProperty("NAME").GetString()));
            AssertSections(root, sections);

            var channel = root.GetProperty("CHANNEL");
            Assert.False(string.IsNullOrEmpty(channel.GetProperty("NAME").GetString()));
            AssertSections(channel, new[] { "ID" }.Concat(RefossDataPoints.Channel.Select(d => d.I18N)));
        }

        private static void AssertSections(JsonElement root, System.Collections.Generic.IEnumerable<string> sections)
        {
            foreach (var section in sections)
            {
                Assert.True(root.TryGetProperty(section, out var s), section);
                Assert.False(string.IsNullOrEmpty(s.GetProperty("NAME").GetString()), section);
                Assert.True(s.TryGetProperty("DESCRIPTION", out _), section);
            }
        }
    }
}
