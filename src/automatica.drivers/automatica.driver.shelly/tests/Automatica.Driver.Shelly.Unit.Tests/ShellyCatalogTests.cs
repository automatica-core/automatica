using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Automatica.Driver.ShellyFactory;
using Automatica.Driver.ShellyFactory.Catalog;
using Automatica.Driver.ShellyFactory.Generic;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    /// <summary>
    /// The component catalog behind the generic templates: stable unique ids, unique keys, every text translated.
    /// </summary>
    public class ShellyCatalogTests
    {
        private static IEnumerable<ShellyComponentType> All => ShellyGenericTemplates.AllComponents;

        [Fact]
        public void TemplateIds_AreUnique()
        {
            var ids = new List<Guid>
            {
                ShellyGenericTemplates.Gen1DeviceTemplate, ShellyGenericTemplates.Gen1DeviceInterface,
                ShellyGenericTemplates.RpcDeviceTemplate, ShellyGenericTemplates.RpcDeviceInterface,
                ShellyFactory.ShellyFactory.DriverId
            };
            foreach (var component in All)
            {
                ids.Add(component.Template);
                ids.Add(component.Interface);
                ids.AddRange(component.DataPoints.Select(a => a.Template));
            }

            var duplicates = ids.GroupBy(a => a).Where(a => a.Count() > 1).Select(a => a.Key).ToList();
            Assert.Empty(duplicates);
        }

        [Fact]
        public void TemplateIds_AreStableAcrossStarts()
        {
            // the ids end up in the database, they must never change for the same component and key
            Assert.Equal(new Guid("6f3c2a41-9d0e-4c57-8a0b-2f41e5c7d913"), ShellyRpcCatalog.Namespace);
            Assert.Equal(ShellyGuid.Create(ShellyRpcCatalog.Namespace, "switch"), ShellyRpcCatalog.Get("switch")!.Template);
            Assert.Equal(ShellyGuid.Create(ShellyGuid.Create(ShellyRpcCatalog.Namespace, "switch"), "output"),
                ShellyRpcCatalog.Get("switch")!.GetDataPoint("output")!.Template);
        }

        [Fact]
        public void TemplateKeys_AreUnique()
        {
            var keys = new List<string> { ShellyGenericTemplates.Gen1DeviceKey, ShellyGenericTemplates.RpcDeviceKey };
            keys.AddRange(All.Select(ShellyGenericTemplates.ComponentKey));

            Assert.Equal(keys.Count, keys.Distinct().Count());
            foreach (var component in All)
            {
                Assert.Equal(component.DataPoints.Count, component.DataPoints.Select(a => a.Key).Distinct().Count());
            }
        }

        [Fact]
        public void EveryWriteableDataPoint_HasAWriterOfItsApi()
        {
            foreach (var component in All)
            {
                foreach (var dp in component.DataPoints)
                {
                    if (component.Api == ShellyApi.Rpc)
                    {
                        Assert.Null(dp.Gen1Writer);
                    }
                    else
                    {
                        Assert.Null(dp.RpcWriter);
                    }

                    if (dp.IsCommand)
                    {
                        Assert.True(dp.IsWriteable, $"{component.Type}.{dp.Key} is a command without a writer");
                    }
                }
            }
        }

        [Fact]
        public void Factory_RegistersEveryGenericTemplate()
        {
            var factory = new Automatica.Core.UnitTests.Drivers.NodeTemplateFactoryMock();
            new ShellyFactory.ShellyFactory().InitNodeTemplates(factory);

            Assert.Equal(ShellyGenericTemplates.Gen1DeviceKey, factory.GetNodeTemplateById(ShellyGenericTemplates.Gen1DeviceTemplate).Key);
            Assert.Equal(ShellyGenericTemplates.RpcDeviceKey, factory.GetNodeTemplateById(ShellyGenericTemplates.RpcDeviceTemplate).Key);
            foreach (var component in All)
            {
                Assert.NotNull(factory.GetNodeTemplateById(component.Template));
                foreach (var dp in component.DataPoints)
                {
                    var template = factory.GetNodeTemplateById(dp.Template);
                    Assert.Equal(dp.Key, template.Key);
                    Assert.Equal(dp.IsWriteable, template.IsWriteable);
                }
            }
        }

        public static IEnumerable<object[]> Languages => new[] { new object[] { "en" }, new object[] { "de" } };

        [Theory]
        [MemberData(nameof(Languages))]
        public void EveryText_IsTranslated(string language)
        {
            var texts = Flatten(LoadResource(language));
            var missing = UsedKeys().Where(a => !texts.ContainsKey(a)).Distinct().OrderBy(a => a).ToList();

            Assert.True(missing.Count == 0, $"{language} misses {string.Join(", ", missing)}");
        }

        private static IEnumerable<string> UsedKeys()
        {
            foreach (var prefix in new[] { "SHELLY.GEN1_DEVICE", "SHELLY.RPC_DEVICE" })
            {
                yield return $"{prefix}.NAME";
                yield return $"{prefix}.DESCRIPTION";
            }

            foreach (var property in new[]
                     {
                         "ID", "IP", "USERNAME", "PASSWORD", "POLLING_INTERVAL", "SLEEPING", "MODEL", "GENERATION", "MODE", "COIOT_DESCRIPTION",
                         "COMPONENT_ID", "SCAN_ADDRESSES", "DEFAULT_USERNAME", "DEFAULT_PASSWORD", "LEARN", "COIOT", "OUTBOUND_WS_PORT"
                     })
            {
                yield return $"SHELLY.PROPERTIES.{property}.NAME";
                yield return $"SHELLY.PROPERTIES.{property}.DESCRIPTION";
            }

            foreach (var component in All)
            {
                yield return $"SHELLY.COMPONENT.{component.I18N}.NAME";
                yield return $"SHELLY.COMPONENT.{component.I18N}.DESCRIPTION";
                foreach (var dp in component.DataPoints)
                {
                    yield return $"SHELLY.DP.{dp.I18N}.NAME";
                    yield return $"SHELLY.DP.{dp.I18N}.DESCRIPTION";
                }
            }
        }

        private static JObject LoadResource(string language)
        {
            var assembly = typeof(ShellyFactory.ShellyFactory).Assembly;
            var name = assembly.GetManifestResourceNames().Single(a => a.EndsWith($"Shelly-{language}.json", StringComparison.Ordinal));
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return JObject.Parse(reader.ReadToEnd());
        }

        private static Dictionary<string, string> Flatten(JObject json)
        {
            var ret = new Dictionary<string, string>();
            foreach (var value in json.Descendants().OfType<JValue>())
            {
                ret[value.Path] = value.ToString();
            }
            return ret;
        }
    }
}
