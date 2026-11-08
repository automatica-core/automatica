using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.Tests.Automation;
using Automatica.Core.Tests.Dispatcher.Utils;
using Automatica.Core.UnitTests.Base.Common;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{
    /// <summary>
    /// Values the visualization writes to a logic input or parameter reach the logic, also for logics added after the engine loaded.
    /// </summary>
    public class LogicVisualizationRegistrationTests : BaseDispatcherTest
    {
        public LogicVisualizationRegistrationTests() : base(new DispatcherMock(), new Mock<IRemanentHandler>().Object)
        {
        }

        private sealed class VisuSource : IDispatchable
        {
            public VisuSource(Guid id) => Id = id;
            public DispatchableSource Source => DispatchableSource.Visualization;
            public DispatchableType Type => DispatchableType.Visualization;
            public string Name => "Web";
            public Guid Id { get; }
            public bool IsRemanent => false;
        }

        private static RuleInterfaceInstance Input(LogicMock logic) =>
            logic.Context.RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplateNavigation.Name == "Input");

        private Task WriteFromVisu(RuleInterfaceInstance target, object value) =>
            Dispatcher.DispatchValue(new VisuSource(target.ObjId), new DispatchValue(target.ObjId, DispatchableType.Visualization, value, DateTime.Now, DispatchValueSource.User));

        /// <summary>
        /// Every value that reaches the logic notifies the visualization once for the input interface
        /// </summary>
        private int InputNotifications(RuleInterfaceInstance input) =>
            Mock.Get(RuleNotify).Invocations.Count(a => a.Method.Name == nameof(IRuleInstanceVisuNotify.NotifyValueChanged) &&
                                                         a.Arguments[0] is RuleInterfaceInstance i && i.ObjId == input.ObjId);

        [Fact]
        public async Task LogicKnownAtLoad_ReceivesVisuWrites()
        {
            var logic = await DispatcherHelperUtils.CreateLogicMock("Known", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);
            await LogicEngineDispatcher.Load();

            await WriteFromVisu(Input(logic), 3);

            await AutomationTestHelper.Eventually(() => logic.WriteReceived, "the visu write reaches the logic");
        }

        [Fact]
        public async Task LogicAddedAfterLoad_ReceivesVisuWrites_OnceRegistered()
        {
            await LogicEngineDispatcher.Load();
            var logic = await DispatcherHelperUtils.CreateLogicMock("Added", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);

            await LogicEngineDispatcher.RegisterLogicInterfaces(logic.Context.RuleInstance);
            await WriteFromVisu(Input(logic), 3);

            await AutomationTestHelper.Eventually(() => logic.WriteReceived, "the visu write reaches the logic added at runtime");
        }

        [Fact]
        public async Task RegisterTwiceAndReload_DeliversEachVisuWriteOnce()
        {
            var logic = await DispatcherHelperUtils.CreateLogicMock("Twice", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);
            await LogicEngineDispatcher.Load();
            await LogicEngineDispatcher.RegisterLogicInterfaces(logic.Context.RuleInstance);
            await LogicEngineDispatcher.RegisterLogicInterfaces(logic.Context.RuleInstance);
            await LogicEngineDispatcher.Reload();
            await LogicEngineDispatcher.RegisterLogicInterfaces(logic.Context.RuleInstance);

            var input = Input(logic);
            await WriteFromVisu(input, 3);

            await AutomationTestHelper.Eventually(() => InputNotifications(input) >= 1, "the visu write reaches the logic");
            await Task.Delay(300);
            Assert.Equal(1, InputNotifications(input));
        }

        [Fact]
        public async Task UnlinkedInterface_NoLongerReceivesVisuWrites()
        {
            await LogicEngineDispatcher.Load();
            var logic = await DispatcherHelperUtils.CreateLogicMock("Removed", Dispatcher, LogicInstanceCache, LogicInterfaceInstanceCache, LogicInstancesStore);
            await LogicEngineDispatcher.RegisterLogicInterfaces(logic.Context.RuleInstance);

            // what CoreServer.StopLogic(ruleInstanceId) does when a logic is deleted
            foreach (var logicInterface in logic.Context.RuleInstance.RuleInterfaceInstance)
            {
                await LogicEngineDispatcher.Unlink(logicInterface.ObjId);
            }

            var input = Input(logic);
            await WriteFromVisu(input, 3);
            await Task.Delay(300);

            Assert.False(logic.WriteReceived);
            Assert.Equal(0, InputNotifications(input));
        }

        [Fact]
        public async Task RegisterLogicInterfaces_IgnoresNull()
        {
            await LogicEngineDispatcher.RegisterLogicInterfaces(null);
            await LogicEngineDispatcher.RegisterLogicInterfaces(new RuleInstance { ObjId = Guid.NewGuid(), RuleInterfaceInstance = null });
        }
    }
}
