using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.Abstraction.Plugins.Drivers;
using Automatica.Core.Runtime.Abstraction.Plugins.Logics;
using Automatica.Core.Runtime.Core.Plugins.Drivers;
using Automatica.Core.Runtime.Core.Plugins.Logics;
using Automatica.Core.Runtime.IO;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Model;
using Automatica.Core.UnitTests.Drivers;
using Automatica.Core.UnitTests.Rules;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{
    public class RuleEngineDispatcherTests
    {
        private readonly ILinkCache _linkCache;
        private readonly IDispatcher _dispatcher;
        private readonly ILogicInstancesStore _logicInstancesStore;
        private readonly IDriverNodesStore _driverNodesStore;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInterfaceInstanceCache;

        private readonly IRuleEngineDispatcher _ruleEngineDispatcher;

        public RuleEngineDispatcherTests()
        {
            _linkCache = new LinkCacheMock();

            _dispatcher = DispatcherMock.Instance;

            _logicInstancesStore = new LogicInstanceStore();
            _driverNodesStore = new DriverNodeStore();
            _nodeInstanceCache = new NodeInstanceCacheMock();
            _logicInterfaceInstanceCache = new LogicInterfaceInstanceCacheMock();


            _ruleEngineDispatcher = new RuleEngineDispatcher(_linkCache, _dispatcher, _logicInstancesStore,
                _driverNodesStore, _nodeInstanceCache, _logicInterfaceInstanceCache);
        }

        ~RuleEngineDispatcherTests()
        {
            _ruleEngineDispatcher.Dispose();
        }

        private void CreateLink(Action<Link> action)
        {
            var link = new Link
            {
                ObjId = Guid.NewGuid()
            };

            action.Invoke(link);

            _linkCache.Add(link.ObjId, link);
        }

        [Fact]
        public async Task TestNode2NodeDispatch()
        {
            var source = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Source", _nodeInstanceCache, _driverNodesStore);
            var target = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target", _nodeInstanceCache, _driverNodesStore);

            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInput = target.Children[0].DriverContext.NodeInstance.ObjId;
                a.This2NodeInstance2RulePageOutput = source.Children[0].DriverContext.NodeInstance.ObjId;

            });

            _ruleEngineDispatcher.Load();

            await DispatcherMock.Instance.DispatchValue(source.Children[0], true);

            Assert.True(((DriverNodeMock)target.Children[0]).WriteReceived);
            var value = DispatcherMock.Instance.GetValue(DispatchableType.NodeInstance, source.Children[0].Id);

            Assert.IsType<bool>(value);
            Assert.True((bool)value);
        }

        [Fact]
        public async Task TestNode2RuleDispatch()
        {
            var source = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Source", _nodeInstanceCache, _driverNodesStore);
            var target = await DispatcherHelperUtils.CreateLogicMock("Target", null, _logicInterfaceInstanceCache, _logicInstancesStore);

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInput = target.Context.RuleInstance.RuleInterfaceInstance.Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input").ObjId;
                a.This2NodeInstance2RulePageOutput = source.Children[0].DriverContext.NodeInstance.ObjId;
            });

            _ruleEngineDispatcher.Load();

            await DispatcherMock.Instance.DispatchValue(source.Children[0], true);

            Assert.True(target.WriteReceived);
            var value = DispatcherMock.Instance.GetValue(DispatchableType.NodeInstance, source.Children[0].Id);

            Assert.IsType<bool>(value);
            Assert.True((bool)value);
        }
        
        [Fact]
        public async Task TestRule2RuleDispatch()
        {
            var source = await DispatcherHelperUtils.CreateLogicMock("Source", null, _logicInterfaceInstanceCache, _logicInstancesStore);
            var target = await DispatcherHelperUtils.CreateLogicMock("Target", null, _logicInterfaceInstanceCache, _logicInstancesStore);

            var outputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");
            var inputInterface = target.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input");

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInput = inputInterface.ObjId;
                a.This2RuleInterfaceInstanceOutput = outputInterface.ObjId;
            });

            _ruleEngineDispatcher.Load();

            await DispatcherMock.Instance.DispatchValue(new RuleInterfaceInstanceDispatchable(outputInterface), true);

            Assert.True(target.WriteReceived);
            var value = DispatcherMock.Instance.GetValue(DispatchableType.RuleInstance, outputInterface.ObjId);

            Assert.IsType<bool>(value);
            Assert.True((bool)value);
        }


        [Fact]
        public async Task TestRule2NodeDispatch()
        {
            var source = await DispatcherHelperUtils.CreateLogicMock("Source", null, _logicInterfaceInstanceCache, _logicInstancesStore);
            var target = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target", _nodeInstanceCache, _driverNodesStore);

            var outputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");
            
            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInput = target.Children[0].Id;
                a.This2RuleInterfaceInstanceOutput = outputInterface.ObjId;
            });

            _ruleEngineDispatcher.Load();

            await DispatcherMock.Instance.DispatchValue(new RuleInterfaceInstanceDispatchable(outputInterface), true);

            Assert.True(((DriverNodeMock)target.Children[0]).WriteReceived);
            var value = DispatcherMock.Instance.GetValue(DispatchableType.RuleInstance, outputInterface.ObjId);

            Assert.IsType<bool>(value);
            Assert.True((bool)value);
        }
    }
}
