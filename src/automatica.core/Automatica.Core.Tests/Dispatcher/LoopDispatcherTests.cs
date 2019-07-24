using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.Tests.Dispatcher.Utils;
using Automatica.Core.UnitTests.Base.Common;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{
    public class LoopDispatcherTests : BaseDispatcherTest
    {
      
        public LoopDispatcherTests() : base(new DispatcherLoopCheckMock())
        {
             }

        [Fact]
        public async Task TestLoop()
        {
            var source = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Source", Dispatcher, NodeInstanceCache, DriverNodesStore);
            var target = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target", Dispatcher, NodeInstanceCache, DriverNodesStore);
            var target2 = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "Target_Loop_Back", Dispatcher, NodeInstanceCache, DriverNodesStore);

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

            await Assert.ThrowsAsync<LockRecursionException>(async () =>
            {
                await Dispatcher.DispatchValue(source.Children[0], true);
            });

        }
    }
}
