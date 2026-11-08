using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using Automatica.Logic.Metering.EnergyMonitor;
using Automatica.Logic.Metering.Meters;
using Automatica.Logic.Metering.Tests.EnergyMonitor;
using Xunit;

namespace Automatica.Logic.Metering.Tests.Meters
{
    /// <summary>checks over all logics of the plugin: ids, parameter keys and texts</summary>
    public class MeteringFactoriesTests
    {
        public static IEnumerable<object[]> Factories() =>
            typeof(MeterLogicFactory).Assembly.GetTypes()
                .Where(t => typeof(LogicFactory).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => new object[] { t.Name });

        private static LogicFactory Create(string name) =>
            (LogicFactory)Activator.CreateInstance(typeof(MeterLogicFactory).Assembly.GetTypes().Single(t => t.Name == name));

        [Fact]
        public void ThePluginHasTheMonitorAndSixMeters()
        {
            Assert.Equal(7, Factories().Count());
        }

        [Fact]
        public void AllIds_AreUnique()
        {
            var ids = new List<Guid>();
            foreach (var row in Factories())
            {
                var factory = Create((string)row[0]);
                ids.Add(factory.LogicGuid);
                foreach (var (method, args) in EnergyMonitorLogicFactoryTests.RecordingFactory.Record(factory))
                {
                    if (method.Name.StartsWith("Create") && method.Name.Contains("InterfaceTemplate"))
                    {
                        ids.Add((Guid)args[0]);
                    }
                }
            }

            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void TheMeterKeepsTheIdOfTheFormerPlaceholder()
        {
            Assert.Equal(new Guid("6b395c46-ee29-4f06-b416-c9367c41fc85"), new MeterLogicFactory().LogicGuid);
            // higher than the placeholder's 0.3.1.1, so the core creates the new interfaces for existing instances
            Assert.True(new MeterLogicFactory().LogicVersion > new Version(0, 3, 1, 1));
        }

        [Theory]
        [MemberData(nameof(Factories))]
        public void Parameters_UseTheOverloadThatKeepsTheKey(string name)
        {
            foreach (var (method, _) in EnergyMonitorLogicFactoryTests.RecordingFactory.Record(Create(name))
                         .Where(c => c.Method.Name == nameof(ILogicTemplateFactory.CreateParameterLogicInterfaceTemplate)))
            {
                Assert.Contains(method.GetParameters(), p => p.Name == "linkable");
            }
        }

        [Theory]
        [MemberData(nameof(Factories))]
        public void AllTexts_ExistInEnglishAndGerman(string name)
        {
            var texts = new List<string>();
            foreach (var (method, args) in EnergyMonitorLogicFactoryTests.RecordingFactory.Record(Create(name)))
            {
                var parameters = method.GetParameters();
                foreach (var key in new[] { "name", "description" })
                {
                    var i = Array.FindIndex(parameters, p => p.Name == key);
                    if (i >= 0 && args[i] is string text && text.StartsWith("METERING."))
                    {
                        texts.Add(text);
                    }
                }
            }
            Assert.NotEmpty(texts);

            foreach (var language in new[] { "en", "de" })
            {
                using var stream = typeof(MeterLogicFactory).Assembly.GetManifestResourceStream($"Automatica.Logic.Metering.Resources.Automatica.Logic.Metering-{language}.json");
                Assert.NotNull(stream);
                using var json = JsonDocument.Parse(stream);
                var missing = texts.Where(t => !Resolve(json.RootElement, t)).ToList();
                Assert.True(missing.Count == 0, $"{language}: {string.Join(", ", missing)}");
            }
        }

        private static bool Resolve(JsonElement root, string path)
        {
            var current = root;
            foreach (var part in path.Split('.'))
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
                {
                    return false;
                }
            }
            return current.ValueKind == JsonValueKind.String && current.GetString().Length > 0;
        }
    }

    public class FileMeterStateStoreTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "metering-store-" + Guid.NewGuid().ToString("N"));

        [Fact]
        public void SavesAndLoads()
        {
            var store = new FileMeterStateStore(_directory);
            var id = Guid.NewGuid();
            var state = new MeterState();
            state.Registers["energy"] = new MeterRegisterState { Today = 1.5, Month = 20, Total = 1234.5, Day = "2026-03-10", LastReading = 1234.5, ReadingMode = true };
            state.Values["pulseCounter"] = 77;

            store.Save(id, state);
            var loaded = store.Load(id);

            Assert.Equal(1234.5, loaded.Registers["energy"].Total);
            Assert.True(loaded.Registers["energy"].ReadingMode);
            Assert.Equal(77, loaded.Values["pulseCounter"]);
            Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        }

        [Fact]
        public void Missing_IsNull()
        {
            Assert.Null(new FileMeterStateStore(_directory).Load(Guid.NewGuid()));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }
    }
}
