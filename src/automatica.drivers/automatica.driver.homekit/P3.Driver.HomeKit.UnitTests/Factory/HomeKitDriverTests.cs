using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory;
using P3.Driver.HomeKitFactory.NodeInstances;
using P3.Driver.HomeKitFactory.NodeInstances.Nodes;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Factory
{
    public class HomeKitDriverTests : DriverFactoryTestBase<HomeKitFactory.HomeKitFactory>
    {
        private DriverContextMock Context(string templateKey)
        {
            var node = Factory.CreateNodeInstanceByKey(templateKey);
            node.ObjId = Guid.NewGuid();
            return new DriverContextMock(node, DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
        }

        [Fact]
        public void LightBulbBrightnessUsesTheExistingCharacteristic()
        {
            var driverNode = CreateNodeInstance(DriverFactory.DriverGuid);
            var driver = new HomeKitDriver(new DriverContextMock(driverNode, DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System));

            var accessory = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "AutomaticaCore", "1", false, 0);
            var folder = new FolderNodeInstance(Context("light-bulb-folder"), driver, accessory);

            var node = (BaseNode)folder.CreateDriverNode(Context("light-bulb-brightness"));

            var brightness = accessory.Specific.Characteristics.Where(a => a.Type == CharacteristicBase.BrightnessType).ToList();
            Assert.Single(brightness);
            Assert.Same(brightness[0], node.Characteristic);
            Assert.Equal(2, accessory.Specific.Characteristics.Count);
        }
    }
}
