using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Rule;
using Automatica.Core.Tests.Dispatcher.Utils;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{
    public class LoopDispatcherTests : BaseDispatcherTest
    {
      
        public LoopDispatcherTests() : base(new DispatcherLoopCheckMock())
        {
             }

        [Fact]
        public async Task TestLoopNodes()
        {
            var source = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Source", Dispatcher,
                NodeInstanceCache, DriverNodesStore);
            var target = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target", Dispatcher,
                NodeInstanceCache, DriverNodesStore);
            var target2 = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target_Loop_Back", Dispatcher,
                NodeInstanceCache, DriverNodesStore);

            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInput = target.Children[0].DriverContext.NodeInstance.ObjId;
                a.This2NodeInstance2RulePageOutput = source.Children[0].DriverContext.NodeInstance.ObjId;

            });

            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInput = target2.Children[0].DriverContext.NodeInstance.ObjId;
                a.This2NodeInstance2RulePageOutput = target.Children[0].DriverContext.NodeInstance.ObjId;

            });
            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInput = source.Children[0].DriverContext.NodeInstance.ObjId;
                a.This2NodeInstance2RulePageOutput = target2.Children[0].DriverContext.NodeInstance.ObjId;

            });

            RuleEngineDispatcher.Load();

            await Dispatcher.DispatchValue(source.Children[0], true);
        }

        [Fact]
        public async Task TestLoopRules()
        {
            var source = await DispatcherHelperUtils.CreateLogicMock("Source", Dispatcher, null, LogicInterfaceInstanceCache, LogicInstancesStore);
            var target = await DispatcherHelperUtils.CreateLogicMock("Target", Dispatcher, null, LogicInterfaceInstanceCache, LogicInstancesStore);
            var target2 = await DispatcherHelperUtils.CreateLogicMock("Target_Loop_Back", Dispatcher, null, LogicInterfaceInstanceCache, LogicInstancesStore);

            var inputInterfaceSource = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input");
            var outputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");

            var inputInterface = target.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input");
            var outputInterface2 = target.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");

            var inputInterfaceLoop = target2.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input");
            var outputInterfaceLoop = target2.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInput = inputInterface.ObjId;
                a.This2RuleInterfaceInstanceOutput = outputInterface.ObjId;
            });

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInput = inputInterfaceLoop.ObjId;
                a.This2RuleInterfaceInstanceOutput = outputInterface2.ObjId;
            });

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInput = inputInterfaceSource.ObjId;
                a.This2RuleInterfaceInstanceOutput = outputInterfaceLoop.ObjId;
            });

            RuleEngineDispatcher.Load();

            await Dispatcher.DispatchValue(new RuleInterfaceInstanceDispatchable(outputInterface), true);
        }

        [Fact]
        public async Task TestLoopNode2Rule()
        {
            var source = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Source", Dispatcher, NodeInstanceCache, DriverNodesStore);
            var target = await DispatcherHelperUtils.CreateLogicMock("Target", Dispatcher, null, LogicInterfaceInstanceCache, LogicInstancesStore);

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInput = target.Context.RuleInstance.RuleInterfaceInstance.Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input").ObjId;
                a.This2NodeInstance2RulePageOutput = source.Children[0].DriverContext.NodeInstance.ObjId;
            });

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceOutput= target.Context.RuleInstance.RuleInterfaceInstance.Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output").ObjId;
                a.This2NodeInstance2RulePageInput = source.Children[0].DriverContext.NodeInstance.ObjId;
            });

            RuleEngineDispatcher.Load();

            await Dispatcher.DispatchValue(source.Children[0], true);

        }

        [Fact]
        public async Task TestLoopRule2Node()
        {
            var source = await DispatcherHelperUtils.CreateLogicMock("Source", Dispatcher, null, LogicInterfaceInstanceCache, LogicInstancesStore);
            var target = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target", Dispatcher, NodeInstanceCache, DriverNodesStore);

            var outputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");

            var inputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input");

            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInput = target.Children[0].Id;
                a.This2RuleInterfaceInstanceOutput = outputInterface.ObjId;
            });
            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageOutput = target.Children[0].Id;
                a.This2RuleInterfaceInstanceInput = inputInterface.ObjId;
            });

            RuleEngineDispatcher.Load();

            await Dispatcher.DispatchValue(new RuleInterfaceInstanceDispatchable(outputInterface), true);
        }
    }
}
