using System.Text.Json;
using P3.Driver.Matter.Driver;

namespace P3.Driver.Matter.Tests
{
    public class DeviceTemplateTests
    {
        private static IEnumerable<(Guid Guid, string Key)> AllTemplates() =>
            MatterDeviceKinds.All.SelectMany(kind => new[] { (MatterDeviceTemplates.FolderGuid(kind), MatterDeviceTemplates.FolderKey(kind)) }
                .Concat(kind.Points.Select(p => (MatterDeviceTemplates.PointGuid(kind, p), MatterDeviceTemplates.PointKey(kind, p)))));

        [Fact]
        public void Template_guids_and_keys_are_unique_and_stable()
        {
            var templates = AllTemplates().ToList();
            Assert.Equal(templates.Count, templates.Select(t => t.Guid).Distinct().Count());
            Assert.Equal(templates.Count, templates.Select(t => t.Key).Distinct().Count());

            // RFC 4122 v5 and fixed: changing the algorithm would orphan existing nodes
            var guid = MatterDeviceTemplates.FolderGuid(MatterDeviceKinds.Thermostat);
            Assert.Equal(5, guid.ToByteArray()[7] >> 4);
            Assert.Equal(new Guid("a1d0757b-614c-574f-ab31-539fffcc387a"), guid);
        }

        [Fact]
        public void Template_keys_resolve_back_to_kinds_and_points()
        {
            foreach (var kind in MatterDeviceKinds.All)
            {
                Assert.Same(kind, MatterDeviceTemplates.KindForFolder(MatterDeviceTemplates.FolderKey(kind)));
                foreach (var point in kind.Points)
                {
                    Assert.Equal(point.Key, MatterDeviceTemplates.PointForTemplate(kind, MatterDeviceTemplates.PointKey(kind, point)));
                }
            }

            Assert.Null(MatterDeviceTemplates.KindForFolder(MatterDriverFactory.PairingCodeNodeKey));
        }

        [Theory]
        [InlineData("Matter-en.json")]
        [InlineData("Matter-de.json")]
        public void Every_template_is_translated(string file)
        {
            var asm = typeof(MatterDriverFactory).Assembly;
            var name = asm.GetManifestResourceNames().Single(n => n.EndsWith(file, StringComparison.Ordinal));
            using var stream = asm.GetManifestResourceStream(name)!;
            using var doc = JsonDocument.Parse(stream);

            var keys = MatterDeviceKinds.All.Select(MatterDeviceTemplates.KindNameKey)
                .Concat(MatterDeviceKinds.All.SelectMany(k => k.Points).Select(MatterDeviceTemplates.PointNameKey)).Distinct();
            foreach (var key in keys)
            {
                var element = doc.RootElement;
                foreach (var part in key.Split('.'))
                {
                    Assert.True(element.TryGetProperty(part, out element), $"{file}: {key} missing");
                }

                Assert.False(string.IsNullOrWhiteSpace(element.GetProperty("NAME").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(element.GetProperty("DESCRIPTION").GetString()));
            }
        }
    }
}
