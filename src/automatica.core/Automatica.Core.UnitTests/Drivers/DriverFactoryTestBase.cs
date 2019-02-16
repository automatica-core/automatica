using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace Automatica.Core.UnitTests.Drivers
{
    public abstract class DriverFactoryTestBase<T> where T : IDriverFactory
    {
        protected NodeTemplateFactoryMock Factory { get; }
        protected T DriverFactory { get; }
        
        protected DriverFactoryTestBase()
        {
            Factory = new NodeTemplateFactoryMock();

            DriverFactory = (T)Activator.CreateInstance(typeof(T));

            DriverFactory.InitNodeTemplates(Factory);
        }

        protected NodeInstance CreateNodeInstance(Guid guid)
        {
          var node= Factory.CreateNodeInstance(guid);
            node.ObjId = Guid.NewGuid();
            return node;
        }

        protected IDriver CreateDriver(NodeInstance node)
        {
            var driverContext = new DriverContextMock(node, Factory);
            var driver = DriverFactory.CreateDriver(driverContext);

            driver.Configure();
            return driver;

        }

        public T2 CreateDriver<T2>(NodeInstance node)
        {
            var driver = CreateDriver(node);

            if (driver.GetType() != typeof(T2))
            {
                throw new ArgumentException("Invalid type");
            }
            driver.Start();

            return (T2)driver;
        }

        public T2 CreateDriver<T2>(NodeInstance node, params Guid[] childNodeGuid)
        {
            foreach (var gu in childNodeGuid)
            {
                var childNode = CreateNodeInstance(gu);
                childNode.ObjId = Guid.NewGuid();
                node.InverseThis2ParentNodeInstanceNavigation.Add(childNode);
            }

            return CreateDriver<T2>(node);
        }

        public T2 CreateDriver<T2>(Guid driverGuid, params Guid[] childNodeGuid) where T2 : IDriver
        {
            var driverNode = CreateNodeInstance(driverGuid);
            return CreateDriver<T2>(driverNode, childNodeGuid);

        }

        protected virtual void Init()
        {
            
        }
    }
}
