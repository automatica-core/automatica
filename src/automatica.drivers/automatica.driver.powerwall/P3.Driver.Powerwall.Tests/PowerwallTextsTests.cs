using System.IO;
using System.Linq;
using System.Text.Json;
using P3.Driver.Powerwall.Driver;
using Xunit;

namespace P3.Driver.Powerwall.Tests
{
    /// <summary>
    /// Every translation key of the driver exists in the English and the German resource file
    /// </summary>
    public class PowerwallTextsTests
    {
        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void AllKeysExist(string lang)
        {
            var assembly = typeof(PowerwallDriverFactory).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($"Powerwall-{lang}.json"));
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
            using var doc = JsonDocument.Parse(reader.ReadToEnd());
            var root = doc.RootElement.GetProperty("POWERWALL");

            var sections = new[] { "HOST", "PASSWORD", "EMAIL", "POLL", "ALLOW_WRITES", "FINGERPRINT" }
                .Concat(PowerwallDataPoints.All.Select(d => d.I18N));
            Assert.False(string.IsNullOrEmpty(root.GetProperty("NAME").GetString()));
            foreach (var section in sections)
            {
                Assert.True(root.TryGetProperty(section, out var s), section);
                Assert.False(string.IsNullOrEmpty(s.GetProperty("NAME").GetString()), section);
                Assert.True(s.TryGetProperty("DESCRIPTION", out _), section);
            }
        }
    }
}
