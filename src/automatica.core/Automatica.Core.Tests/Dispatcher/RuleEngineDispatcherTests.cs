using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Automatica.Core.Tests.Dispatcher.Utils;
using Automatica.Core.UnitTests.Base.Common;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{
    public class RuleEngineDispatcherTests : BaseDispatcherTest
    {

        public RuleEngineDispatcherTests() : base(DispatcherMock.Instance)
        {
        }

        [Fact]
        public async Task TestNode2NodeDispatch()
        {
            var source = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Source", Dispatcher,
                NodeInstanceCache, DriverNodesStore);
            var target = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target", Dispatcher,
                NodeInstanceCache, DriverNodesStore);

            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance = target.Children[0].DriverContext.NodeInstance.ObjId
                };
                a.This2NodeInstance2RulePageOutputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance = source.Children[0].DriverContext.NodeInstance.ObjId
                };

            });

            RuleEngineDispatcher.Load();

            await Dispatcher.DispatchValue(source.Children[0], true);
            await Task.Delay(200);

            Assert.True(((DriverNodeMock) target.Children[0]).WriteReceived);
            var value = Dispatcher.GetValue(DispatchableType.NodeInstance, source.Children[0].Id);

            Assert.IsType<bool>(value);
            Assert.True((bool) value);
        }

        [Fact]
        public async Task TestNode2RuleDispatch()
        {
            var source = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Source", Dispatcher,
                NodeInstanceCache, DriverNodesStore);
            var target = await DispatcherHelperUtils.CreateLogicMock("Target", Dispatcher, null,
                LogicInterfaceInstanceCache, LogicInstancesStore);

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInputNavigation = new RuleInterfaceInstance()
                {
                    ObjId = target.Context.RuleInstance.RuleInterfaceInstance
                        .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input").ObjId,
                    This2RuleInstance = target.Context.RuleInstance.RuleInterfaceInstance
                            .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input").This2RuleInstance
                };
                a.This2NodeInstance2RulePageOutputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance = source.Children[0].DriverContext.NodeInstance.ObjId
                };
            });

            RuleEngineDispatcher.Load();

            await Dispatcher.DispatchValue(source.Children[0], true);
            await Task.Delay(200);

            Assert.True(target.WriteReceived);
            var value = Dispatcher.GetValue(DispatchableType.NodeInstance, source.Children[0].Id);

            Assert.IsType<bool>(value);
            Assert.True((bool) value);
        }

        [Fact]
        public async Task TestRule2RuleDispatch()
        {
            var source = await DispatcherHelperUtils.CreateLogicMock("Source", Dispatcher, null,
                LogicInterfaceInstanceCache, LogicInstancesStore);
            var target = await DispatcherHelperUtils.CreateLogicMock("Target", Dispatcher, null,
                LogicInterfaceInstanceCache, LogicInstancesStore);

            var outputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");
            var inputInterface = target.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input");

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInputNavigation = new RuleInterfaceInstance()
                {
                    This2RuleInstance = inputInterface.ObjId
                };
                a.This2RuleInterfaceInstanceOutputNavigation = new RuleInterfaceInstance()
                {
                    This2RuleInstance = outputInterface.ObjId
                };
            });

            RuleEngineDispatcher.Load();

            await Dispatcher.DispatchValue(new RuleInterfaceInstanceDispatchable(outputInterface), true);
            await Task.Delay(200);

            Assert.True(target.WriteReceived);
            var value = Dispatcher.GetValue(DispatchableType.RuleInstance, outputInterface.ObjId);

            Assert.IsType<bool>(value);
            Assert.True((bool) value);
        }


        [Fact]
        public async Task TestRule2NodeDispatch()
        {
            var source = await DispatcherHelperUtils.CreateLogicMock("Source", Dispatcher, null,
                LogicInterfaceInstanceCache, LogicInstancesStore);
            var target = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target", Dispatcher,
                NodeInstanceCache, DriverNodesStore);

            var outputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");

            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance = target.Children[0].Id
                };
                a.This2RuleInterfaceInstanceOutputNavigation = new RuleInterfaceInstance()
                {
                    ObjId = source.Context.RuleInstance.RuleInterfaceInstance
                        .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output").ObjId,
                    This2RuleInstance = source.Context.RuleInstance.RuleInterfaceInstance
                        .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output").This2RuleInstance
                };
            });

            RuleEngineDispatcher.Load();

            await Dispatcher.DispatchValue(new RuleInterfaceInstanceDispatchable(outputInterface), true);
            await Task.Delay(200);

            Assert.True(((DriverNodeMock) target.Children[0]).WriteReceived);
            var value = Dispatcher.GetValue(DispatchableType.RuleInstance, outputInterface.ObjId);

            Assert.IsType<bool>(value);
            Assert.True((bool) value);
        }

    }
}