using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.Tests.Dispatcher.Utils;
using Automatica.Core.UnitTests.Base.Logics;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{
    public class LoopDispatcherTests : BaseDispatcherTest
    {

        public LoopDispatcherTests() : base(new DispatcherLoopCheckMock(), new Mock<IRemanentHandler>().Object)
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
                a.This2NodeInstance2RulePageInputNavigation = new NodeInstance2RulePage
                    {
                        This2NodeInstance =
                            target.Children[0].DriverContext.NodeInstance.ObjId
                    }
                    ;
                a.This2NodeInstance2RulePageOutputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance =
                        source.Children[0].DriverContext.NodeInstance.ObjId
                };

            });

            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance =
                        target2.Children[0].DriverContext.NodeInstance.ObjId
                };
                a.This2NodeInstance2RulePageOutputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance =
                        target.Children[0].DriverContext.NodeInstance.ObjId
                };

            });
            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance =
                        source.Children[0].DriverContext.NodeInstance.ObjId
                };
                a.This2NodeInstance2RulePageOutputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance =
                        target2.Children[0].DriverContext.NodeInstance.ObjId
                };

            });

            await LogicEngineDispatcher.Load();

            await Dispatcher.DispatchValue(source.Children[0], true);
        }

        [Fact]
        public async Task TestLoopRules()
        {
            var source = await DispatcherHelperUtils.CreateLogicMock("Source", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);
            var target = await DispatcherHelperUtils.CreateLogicMock("Target", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);
            var target2 = await DispatcherHelperUtils.CreateLogicMock("Target_Loop_Back", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);

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
                a.This2RuleInterfaceInstanceInputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = inputInterface.ObjId
                };
                a.This2RuleInterfaceInstanceOutputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = outputInterface.ObjId
                };
            });

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = inputInterfaceLoop.ObjId
                };
                a.This2RuleInterfaceInstanceOutputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = outputInterface2.ObjId
                };
            });

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = inputInterfaceSource.ObjId
                };
                a.This2RuleInterfaceInstanceOutputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = outputInterfaceLoop.ObjId
                };
            });

            await LogicEngineDispatcher.Load();

            await Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(outputInterface), true);
        }

        [Fact]
        public async Task TestLoopNode2Rule()
        {
            var source = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Source", Dispatcher, NodeInstanceCache, DriverNodesStore);
            var target = await DispatcherHelperUtils.CreateLogicMock("Target", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceInputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = target.Context.RuleInstance.RuleInterfaceInstance.Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input").ObjId
                };
                a.This2NodeInstance2RulePageOutputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance = source.Children[0].DriverContext.NodeInstance.ObjId
                };
            });

            CreateLink(a =>
            {
                a.This2RuleInterfaceInstanceOutputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = target.Context.RuleInstance.RuleInterfaceInstance
                        .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output").ObjId
                };
                a.This2NodeInstance2RulePageInputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance = source.Children[0].DriverContext.NodeInstance.ObjId
                };
            });

            await LogicEngineDispatcher.Load();

            await Dispatcher.DispatchValue(source.Children[0], true);

        }

        [Fact]
        public async Task TestLoopRule2Node()
        {
            var source = await DispatcherHelperUtils.CreateLogicMock("Source", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);
            var target = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target", Dispatcher, NodeInstanceCache, DriverNodesStore);

            var outputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Output");

            var inputInterface = source.Context.RuleInstance.RuleInterfaceInstance
                .Single(b => b.This2RuleInterfaceTemplateNavigation.Name == "Input");

            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageInputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance = target.Children[0].Id
                };
                a.This2RuleInterfaceInstanceOutputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = outputInterface.ObjId
                };
            });
            CreateLink(a =>
            {
                a.This2NodeInstance2RulePageOutputNavigation = new NodeInstance2RulePage
                {
                    This2NodeInstance = target.Children[0].Id
                };
                a.This2RuleInterfaceInstanceInputNavigation = new RuleInterfaceInstance
                {
                    This2RuleInstance = inputInterface.ObjId
                };
            });

            await LogicEngineDispatcher.Load();

            await Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(outputInterface), true);
        }
    }
}
