using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.ModBusDriverFactory;
using P3.Driver.ModBusDriverFactory.Master;
using P3.Driver.ModBusDriverFactory.Slave;

namespace P3.Driver.ModBus.Tests
{
    public class ModBusBaseTest : DriverFactoryTestBase<ModBusTcpMasterDriverFactory>
    {
        public static DispatchValue Create(object value)
        {
            return new DispatchValue(DispatchableMock.Instance.Id, DispatchableType.NodeInstance, value, DateTime.Now,
                DispatchValueSource.Read);
        }

        protected async Task<ModBusMasterAttribute> InitAttribute(Guid attributeType, Func<NodeInstance, NodeInstance> modifyNodeAttribute = null)
        {
            var factory = Factory;

            var modbusDriverFactory = new ModBusTcpMasterDriverFactory();
            modbusDriverFactory.InitNodeTemplates(factory);

            var modbusTcpSlaveDriverFactory = new ModBusTcpSlaveDriverFactory();
            modbusTcpSlaveDriverFactory.InitNodeTemplates(factory);

            var modbusRtuDriverFactory = new ModBusRtuMasterDriverFactory();
            modbusRtuDriverFactory.InitTemplates(factory);

            var modbusRtuSlaveDriverFactory = new ModBusRtuSlaveDriverFactory();
            modbusRtuSlaveDriverFactory.InitTemplates(factory);


            var driverNode = factory.CreateNodeInstance(ModBusTcpMasterDriverFactory.ModBusMasterTcpGuid);
            var device = factory.CreateNodeInstance(ModBusDriverFactory.ModBusDriverFactory.DeviceTemplate);
            var attributeNode = factory.CreateNodeInstance(attributeType);

            if (modifyNodeAttribute != null)
            {
                attributeNode = modifyNodeAttribute(attributeNode);
            }

            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(device);
            device.InverseThis2ParentNodeInstanceNavigation.Add(attributeNode);

            var driverContext = new DriverContextMock(driverNode, DriverFactory, factory, Dispatcher, NullLoggerFactory.Instance);
            var driver = modbusDriverFactory.CreateDriver(driverContext) as ModBusMasterDriver;
            await driver.Configure();

            return driver.Children[0].Children[0] as ModBusMasterAttribute;
        }


        protected async Task<ModBusSlaveAttribute> InitSlaveAttribute(Guid attributeType, Func<NodeInstance, NodeInstance> modifyNodeAttribute = null)
        {
            var factory = new NodeTemplateFactoryMock();

            var modbusDriverFactory = new ModBusTcpMasterDriverFactory();
            modbusDriverFactory.InitNodeTemplates(factory);

            var modbusTcpSlaveDriverFactory = new ModBusTcpSlaveDriverFactory();
            modbusTcpSlaveDriverFactory.InitNodeTemplates(factory);

            var modbusRtuDriverFactory = new ModBusRtuMasterDriverFactory();
            modbusRtuDriverFactory.InitTemplates(factory);

            var modbusRtuSlaveDriverFactory = new ModBusRtuSlaveDriverFactory();
            modbusRtuSlaveDriverFactory.InitTemplates(factory);


            var driverNode = factory.CreateNodeInstance(modbusTcpSlaveDriverFactory.DriverGuid);
            var device = factory.CreateNodeInstance(ModBusDriverFactory.ModBusDriverFactory.DeviceTemplate);

            driverNode.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == "modbus-port")
                .Value = new Random().Next(2000, 5000);

            var attributeNode = factory.CreateNodeInstance(attributeType);

            if (modifyNodeAttribute != null)
            {
                attributeNode = modifyNodeAttribute(attributeNode);
            }

            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(device);
            device.InverseThis2ParentNodeInstanceNavigation.Add(attributeNode);

            var driverContext = new DriverContextMock(driverNode, DriverFactory, factory, Dispatcher, NullLoggerFactory.Instance);
            var driver = modbusTcpSlaveDriverFactory.CreateDriver(driverContext);
            await driver.Init();
            await driver.Configure();

            await driver.Start();

            return driver.Children[0].Children[0] as ModBusSlaveAttribute;
        }
    }
}
