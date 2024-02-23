using System;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.UnitTests.Base.Drivers
{
    public abstract class DriverFactoryTestBase<T> where T : IDriverFactory
    {
        public IDispatcher Dispatcher { get; }
        protected NodeTemplateFactoryMock Factory { get; }
        protected T DriverFactory { get; }
        
        protected DriverFactoryTestBase()
        {
            Dispatcher = DispatcherMock.Instance;
            Factory = new NodeTemplateFactoryMock();

            DriverFactory = (T)Activator.CreateInstance(typeof(T));

            DriverFactory!.InitNodeTemplates(Factory);
        }

        protected NodeInstance CreateNodeInstance(Guid guid)
        {
            var node = Factory.CreateNodeInstance(guid);
            node.ObjId = Guid.NewGuid();
            return node;
        }

        protected async Task<IDriver> CreateDriver(NodeInstance node)
        {
            var driverContext = new DriverContextMock(node, DriverFactory, Factory, Dispatcher, new NullLoggerFactory(), DateTimeHelper.ProviderInstance);
            var driver = DriverFactory.CreateDriver(driverContext);

            await driver.Configure();
            return driver;

        }

        public async Task<T2> CreateDriver<T2>(NodeInstance node)
        {
            var driver = await CreateDriver(node);

            if (driver.GetType() != typeof(T2))
            {
                throw new ArgumentException("Invalid type");
            }
            await driver.Start();
            await driver.Started();

            return (T2)driver;
        }

        public async Task<T2> CreateDriver<T2>(NodeInstance node, params Guid[] childNodeGuid)
        {
            foreach (var gu in childNodeGuid)
            {
                var childNode = CreateNodeInstance(gu);
                childNode.ObjId = Guid.NewGuid();
                node.InverseThis2ParentNodeInstanceNavigation.Add(childNode);
            }

            return await CreateDriver<T2>(node);
        }

        public async Task<T2> CreateDriver<T2>(Guid driverGuid, params Guid[] childNodeGuid) where T2 : IDriver
        {
            var driverNode = CreateNodeInstance(driverGuid);
            return await CreateDriver<T2>(driverNode, childNodeGuid);

        }

        protected virtual void Init()
        {
            
        }
    }
}
