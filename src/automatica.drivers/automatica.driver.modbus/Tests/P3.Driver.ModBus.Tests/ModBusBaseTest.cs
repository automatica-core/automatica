using System;
using System.Net;
using System.Net.Sockets;
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
            modbusRtuDriverFactory.InitNodeTemplates(factory);

            var modbusRtuSlaveDriverFactory = new ModBusRtuSlaveDriverFactory();
            modbusRtuSlaveDriverFactory.InitNodeTemplates(factory);


            var driverNode = factory.CreateNodeInstance(ModBusTcpMasterDriverFactory.ModBusMasterTcpGuid);
            var device = factory.CreateNodeInstance(ModBusDriverFactory.ModBusDriverFactory.DeviceTemplate);
            var attributeNode = factory.CreateNodeInstance(attributeType);

            if (modifyNodeAttribute != null)
            {
                attributeNode = modifyNodeAttribute(attributeNode);
            }

            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(device);
            device.InverseThis2ParentNodeInstanceNavigation.Add(attributeNode);

            var driverContext = new DriverContextMock(driverNode, DriverFactory, factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = modbusDriverFactory.CreateDriver(driverContext) as ModBusMasterDriver;
            await driver.Configure();

            return driver.Children[0].Children[0] as ModBusMasterAttribute;
        }


        /// <summary>
        /// A port the OS reports as free. A random port collided with a slave a previous test left listening
        /// ("Address already in use"), because the slave tests never stop their driver.
        /// </summary>
        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        protected async Task<ModBusSlaveAttribute> InitSlaveAttribute(Guid attributeType, Func<NodeInstance, NodeInstance> modifyNodeAttribute = null)
        {
            var factory = new NodeTemplateFactoryMock();

            var modbusDriverFactory = new ModBusTcpMasterDriverFactory();
            modbusDriverFactory.InitNodeTemplates(factory);

            var modbusTcpSlaveDriverFactory = new ModBusTcpSlaveDriverFactory();
            modbusTcpSlaveDriverFactory.InitNodeTemplates(factory);

            var modbusRtuDriverFactory = new ModBusRtuMasterDriverFactory();
            modbusRtuDriverFactory.InitNodeTemplates(factory);

            var modbusRtuSlaveDriverFactory = new ModBusRtuSlaveDriverFactory();
            modbusRtuSlaveDriverFactory.InitNodeTemplates(factory);


            var driverNode = factory.CreateNodeInstance(modbusTcpSlaveDriverFactory.DriverGuid);
            var device = factory.CreateNodeInstance(ModBusDriverFactory.ModBusDriverFactory.DeviceTemplate);

            driverNode.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == "modbus-port")
                .Value = GetFreeTcpPort();

            var attributeNode = factory.CreateNodeInstance(attributeType);

            if (modifyNodeAttribute != null)
            {
                attributeNode = modifyNodeAttribute(attributeNode);
            }

            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(device);
            device.InverseThis2ParentNodeInstanceNavigation.Add(attributeNode);

            var driverContext = new DriverContextMock(driverNode, DriverFactory, factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = modbusTcpSlaveDriverFactory.CreateDriver(driverContext);
            await driver.Init();
            await driver.Configure();

            await driver.Start();

            return driver.Children[0].Children[0] as ModBusSlaveAttribute;
        }
    }
}
