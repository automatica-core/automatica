using System;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Automatica.Core.Runtime.Core.Plugins.Logics;
using Automatica.Core.Runtime.IO;
using Automatica.Core.Tests.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Automatica.Core.Tests.Dispatcher.Utils
{
    public abstract class BaseDispatcherTest
    {
        protected ILinkCache LinkCache { get; set; }
        protected virtual IDispatcher Dispatcher { get; }
        protected ILogicInstancesStore LogicInstancesStore { get; set; }
        protected IDriverNodesStore DriverNodesStore { get; set; }
        protected INodeInstanceCache NodeInstanceCache { get; set; }
        protected ILogicInterfaceInstanceCache LogicInterfaceInstanceCache { get; set; }

        protected ILogicEngineDispatcher LogicEngineDispatcher { get; set; }
        protected IRuleInstanceVisuNotify RuleNotify { get; set; }

        protected BaseDispatcherTest(IDispatcher dispatcher, IRemanentHandler remanentHandler)
        {
            LinkCache = new LinkCacheMock();

            Dispatcher = dispatcher;

            LogicInstancesStore = new LogicInstanceStore();
            DriverNodesStore = new DriverNodesStore();
            NodeInstanceCache = new NodeInstanceCacheMock();
            LogicInterfaceInstanceCache = new LogicInterfaceInstanceCacheMock();

            var notifyMock = new Mock<IRuleInstanceVisuNotify>();
            RuleNotify = notifyMock.Object;

            LogicEngineDispatcher = new LogicEngineDispatcher(LinkCache, dispatcher, LogicInstancesStore,
                DriverNodesStore, NodeInstanceCache, LogicInterfaceInstanceCache, NullLogger<LogicEngineDispatcher>.Instance, RuleNotify, remanentHandler);
        }


        ~BaseDispatcherTest()
        {
            LogicEngineDispatcher.Dispose();
        }

        protected void CreateLink(Action<Link> action)
        {
            var link = new Link
            {
                ObjId = Guid.NewGuid()
            };

            action.Invoke(link);

            LinkCache.Add(link.ObjId, link);
        }
    }
}
