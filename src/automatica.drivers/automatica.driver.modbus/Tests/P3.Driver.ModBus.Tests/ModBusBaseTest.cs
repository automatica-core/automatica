using System;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using P3.Driver.ModBusDriver.Slave.Tcp;
using P3.Driver.ModBusDriverFactory;
using P3.Driver.ModBusDriverFactory.Master;
using P3.Driver.ModBusDriverFactory.Slave;

namespace P3.Driver.ModBus.Tests
{
    public class ModBusBaseTest : DriverFactoryTestBase<ModBusTcpMasterDriverFactory>
    {
        protected ModBusMasterAttribute InitAttribute(Guid attributeType, Func<NodeInstance, NodeInstance> modifyNodeAttribute = null)
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

            var driverContext = new DriverContextMock(driverNode, factory, Dispatcher);
            var driver = modbusDriverFactory.CreateDriver(driverContext) as ModBusMasterDriver;
            driver.Configure();

            return driver.Children[0].Children[0] as ModBusMasterAttribute;
        }


        protected ModBusSlaveAttribute InitSlaveAttribute(Guid attributeType, Func<NodeInstance, NodeInstance> modifyNodeAttribute = null)
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
            

            var attributeNode = factory.CreateNodeInstance(attributeType);

            if (modifyNodeAttribute != null)
            {
                attributeNode = modifyNodeAttribute(attributeNode);
            }

            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(device);
            device.InverseThis2ParentNodeInstanceNavigation.Add(attributeNode);

            var driverContext = new DriverContextMock(driverNode, factory, Dispatcher);
            var driver = modbusTcpSlaveDriverFactory.CreateDriver(driverContext);
            driver.Init();
            driver.Configure();

            return driver.Children[0].Children[0] as ModBusSlaveAttribute;
        }
    }
}
