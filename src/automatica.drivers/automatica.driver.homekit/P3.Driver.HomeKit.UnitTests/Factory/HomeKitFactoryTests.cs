using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.UnitTests.Base.Drivers;
using Newtonsoft.Json.Linq;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory.Accessories;
using P3.Driver.HomeKitFactory.NodeInstances;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Factory
{
    public class HomeKitFactoryTests : DriverFactoryTestBase<HomeKitFactory.HomeKitFactory>
    {
        /// <summary>
        /// Records every template guid the factory creates (the mock silently ignores duplicates).
        /// </summary>
        public class RecordingFactory : DispatchProxy
        {
            public List<Guid> NodeTemplates { get; } = new();
            public List<Guid> PropertyTemplates { get; } = new();
            public List<Guid> InterfaceTypes { get; } = new();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                switch (targetMethod.Name)
                {
                    case nameof(INodeTemplateFactory.CreateNodeTemplate):
                        NodeTemplates.Add((Guid)args[0]);
                        break;
                    case nameof(INodeTemplateFactory.CreatePropertyTemplate):
                        PropertyTemplates.Add((Guid)args[0]);
                        break;
                    case nameof(INodeTemplateFactory.CreateInterfaceType):
                        InterfaceTypes.Add((Guid)args[0]);
                        break;
                }

                return targetMethod.ReturnType == typeof(void) ? null : Activator.CreateInstance(targetMethod.ReturnType);
            }
        }

        private static RecordingFactory Record()
        {
            var proxy = DispatchProxy.Create<INodeTemplateFactory, RecordingFactory>();
            new HomeKitFactory.HomeKitFactory().InitNodeTemplates(proxy);
            return (RecordingFactory)(object)proxy;
        }

        [Fact]
        public void TemplateGuidsAreUnique()
        {
            var recorded = Record();
            var all = recorded.NodeTemplates.Concat(recorded.PropertyTemplates).ToList();

            Assert.Equal(all.Count, all.Distinct().Count());
            Assert.Equal(recorded.InterfaceTypes.Count, recorded.InterfaceTypes.Distinct().Count());
        }

        [Fact]
        public void ExistingTemplateGuidsAreUnchanged()
        {
            var recorded = Record();
            foreach (var guid in new[]
                     {
                         "c0491f87-83e4-4510-bad2-e21ebbc490d1", "13d10902-a07c-4abf-a331-70ad2f95a184", "6d07b0a9-4635-4404-8254-76079ea7f6ce",
                         "37685cd8-0494-4ce4-8c59-1fddcf669aa3", "a0c1a70f-ca76-40e5-b7a1-74ff62edfd19", "79e7c522-3eaa-49af-8985-44b345e30473",
                         "515b3a15-9bc5-4121-9f89-36abbb7108c8", "a0809786-de41-40b9-ae7f-076862068464", "123fb63d-943e-4c8c-8c59-1586eb79b961",
                         "4d20d8dc-9331-42d7-b1eb-4eab01b26472", "57303efc-252d-4a2f-b5ec-4a6651e9ef0e", "e3d05974-6c44-4ac8-9eea-a29e036de343",
                         "f107b325-e7c4-46dc-ae56-f2b99ecc5dbe", "6e6cd234-b1b6-466f-8bea-2bffba36592f", "a4b7661e-565d-466d-ad46-be2e2628fb7e",
                         "98b69750-964f-487b-9195-175f6d976099", "693e8ded-b488-4991-971a-c2b46c2b1dcd"
                     })
            {
                Assert.Contains(new Guid(guid), recorded.NodeTemplates);
            }
        }

        [Fact]
        public void EveryAccessoryTypeHasTemplates()
        {
            foreach (var accessoryType in HomeKitAccessoryCatalog.AccessoryTypes)
            {
                var folder = CreateNodeInstance(accessoryType.InterfaceGuid);
                Assert.Equal(accessoryType.Key, folder.This2NodeTemplateNavigation.Key);
                Assert.NotNull(folder.GetProperty(HomeKitFactory.HomeKitFactory.AidPropertyKey));

                foreach (var dataPoint in accessoryType.DataPoints)
                {
                    var node = CreateNodeInstance(dataPoint.TemplateGuid);
                    var template = node.This2NodeTemplateNavigation;
                    Assert.Equal(dataPoint.Key, template.Key);
                    Assert.Equal(accessoryType.InterfaceGuid, template.NeedsInterface2InterfacesType);
                    Assert.Equal(dataPoint.Mandatory, template.DefaultCreated);
                    Assert.True(template.IsReadable || template.IsWriteable, dataPoint.Key);
                }
            }

            Assert.NotNull(CreateNodeInstance(HomeKitAccessoryCatalog.GetByKey("valve-folder").InterfaceGuid).GetProperty(HomeKitFactory.HomeKitFactory.ValveTypePropertyKey));
            Assert.NotNull(CreateNodeInstance(DriverFactory.DriverGuid).GetProperty(HomeKitFactory.HomeKitFactory.SwitchControlTypePropertyKey));
        }

        [Fact]
        public void DataPointKeysAreUnique()
        {
            var keys = HomeKitAccessoryCatalog.AccessoryTypes.Select(a => a.Key)
                .Concat(HomeKitAccessoryCatalog.AccessoryTypes.SelectMany(a => a.DataPoints).Select(a => a.Key))
                .Concat(new[] { "light-bulb-folder", "power-outlet-folder", "contact-sensor-folder", "switch-folder", "temperature-sensor-folder", "switch", "contact-sensor", "temperature-sensor" })
                .ToList();
            Assert.Equal(keys.Count, keys.Distinct().Count());
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void EveryKeyIsTranslated(string language)
        {
            var assembly = typeof(HomeKitFactory.HomeKitFactory).Assembly;
            var name = assembly.GetManifestResourceNames().Single(a => a.EndsWith($"HomeKit-{language}.json", StringComparison.Ordinal));
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!, Encoding.UTF8);
            var json = JObject.Parse(reader.ReadToEnd());

            var keys = HomeKitAccessoryCatalog.AccessoryTypes.SelectMany(a => new[] { a.NameKey, a.DescriptionKey }
                    .Concat(a.DataPoints.SelectMany(d => new[] { d.NameKey, d.DescriptionKey })))
                .Concat(EnumKeys(typeof(HomeKitValveType)))
                .Concat(EnumKeys(typeof(HomeKitSwitchControlType)))
                .Concat(new[] { "APPLE_HOMEKIT_SERVER.VALVE.VALVE_TYPE.NAME", "APPLE_HOMEKIT_SERVER.SWITCH_CONTROL_TYPE.NAME" });

            var missing = keys.Distinct().Where(a => json.SelectToken(a) == null).ToList();
            Assert.True(missing.Count == 0, string.Join(", ", missing));
            Assert.Equal(language == "de" ? "Präsenzmelder" : "Occupancy sensor", (string)json.SelectToken("APPLE_HOMEKIT_SERVER.OCCUPANCY_SENSOR.NAME"));
        }

        private static IEnumerable<string> EnumKeys(Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.GetCustomAttribute<EnumNameAttribute>().TranslationKey);

        [Fact]
        public void CatalogCharacteristicsAreValidHap()
        {
            foreach (var accessoryType in HomeKitAccessoryCatalog.AccessoryTypes)
            {
                var accessory = accessoryType.CreateAccessory(10, accessoryType.Key, "test", "serial", null);
                Assert.Equal(accessoryType.ServiceType, accessory.Specific.Type);

                foreach (var dataPoint in accessoryType.DataPoints)
                {
                    var characteristic = HomeKitAccessoryType.GetCharacteristic(accessory, dataPoint);
                    Assert.Equal(dataPoint.Characteristic.Type, characteristic.Type);
                }

                var ids = accessory.Services.Select(a => a.Id).Concat(accessory.Services.SelectMany(a => a.Characteristics).Select(a => a.Id)).ToList();
                Assert.Equal(ids.Count, ids.Distinct().Count());

                foreach (var service in accessory.Services)
                {
                    var types = service.Characteristics.Select(a => a.Type).ToList();
                    Assert.Equal(types.Count, types.Distinct().Count());
                }
            }
        }

        [Fact]
        public async Task Driver_CreatesAccessoryFromCatalog()
        {
            var thermostat = HomeKitAccessoryCatalog.GetByKey("thermostat-folder");
            var driverNode = CreateNodeInstance(DriverFactory.DriverGuid);
            var folder = CreateNodeInstance(thermostat.InterfaceGuid);
            folder.GetProperty(HomeKitFactory.HomeKitFactory.AidPropertyKey).ValueDouble = 7;
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(folder);

            foreach (var dataPoint in thermostat.DataPoints)
            {
                var child = CreateNodeInstance(dataPoint.TemplateGuid);
                folder.InverseThis2ParentNodeInstanceNavigation.Add(child);
            }

            var driver = (HomeKitFactory.HomeKitDriver)await CreateDriver(driverNode);
            var folderNode = Assert.IsType<FolderNodeInstance>(Assert.Single(driver.Children));

            Assert.Equal(thermostat.DataPoints.Count, folderNode.Children.Count);
            Assert.Equal(7ul, folderNode.Accessory.Id);
            Assert.Equal(new[] { "0F", "33", "11", "35", "36", "10" }, folderNode.Accessory.Specific.Characteristics.Select(a => a.Type));
        }

        [Fact]
        public async Task Driver_ValveTypeFromProperty()
        {
            var valve = HomeKitAccessoryCatalog.GetByKey("valve-folder");
            var driverNode = CreateNodeInstance(DriverFactory.DriverGuid);
            var folder = CreateNodeInstance(valve.InterfaceGuid);
            folder.GetProperty(HomeKitFactory.HomeKitFactory.ValveTypePropertyKey).Value = (int)HomeKitValveType.Irrigation;
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(folder);

            var driver = (HomeKitFactory.HomeKitDriver)await CreateDriver(driverNode);
            var folderNode = Assert.IsType<FolderNodeInstance>(Assert.Single(driver.Children));

            var valveType = folderNode.Accessory.Specific.Characteristics.Single(a => a.Type == CharacteristicBase.ValveTypeType);
            Assert.Equal(1, valveType.Value);
        }
    }
}
