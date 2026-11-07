using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Moq;
using Xunit;
using static Automatica.Core.Tests.LogicPages.LogicPageHarness;

namespace Automatica.Core.Tests.LogicPages
{
    /// <summary>
    /// Logic pages built in code (logic instances, driver nodes, links) on the real dispatcher and logic engine
    /// </summary>
    public sealed class LogicPagePropagationTests : IDisposable
    {
        private readonly LogicPageHarness _page = new();

        public void Dispose() => _page.Dispose();

        private sealed class VisuDispatchable : IDispatchable
        {
            public VisuDispatchable(Guid id) => Id = id;
            public DispatchableSource Source => DispatchableSource.Visualization;
            public DispatchableType Type => DispatchableType.Visualization;
            public string Name => "visu";
            public Guid Id { get; }
            public bool IsRemanent => false;
        }

        private PassThroughLogic AddPassThrough(string name, bool ignoreDuplicates = false) =>
            _page.AddLogic(name, ctx => new PassThroughLogic(ctx, ignoreDuplicates), new[] { "I" }, new[] { "O" });

        [Fact]
        public async Task NodeToNode_ReadValue_IsWrittenToTheTarget()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            _page.LinkNodeToNode(source, target);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, 42);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(42, target.Written.Single());
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task NodeToNode_InvertedTarget_InvertsBooleans(bool sent, bool expected)
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            _page.LinkNodeToNode(source, target, invertTarget: true);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, sent);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(expected, target.Written.Single());
        }

        [Fact]
        public async Task NodeToNode_InvertedTarget_LeavesNonBooleansUntouched()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            _page.LinkNodeToNode(source, target, invertTarget: true);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, 7);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(7, target.Written.Single());
        }

        [Theory]
        [InlineData(DispatchValueSource.Write)]
        [InlineData(DispatchValueSource.User)]
        public async Task NodeToNode_ValuesThatWereNotRead_AreNotForwarded(DispatchValueSource valueSource)
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            _page.LinkNodeToNode(source, target);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true, valueSource);
            await _page.Dispatcher.DispatchValue(source, 99);

            await WaitUntil(() => target.Written.Count >= 1);
            await Task.Delay(50);
            Assert.Equal(new object[] { 99 }, target.Written.ToArray());
        }

        [Fact]
        public async Task SharedSource_InvertedAndPlainLinks_DoNotAffectEachOther()
        {
            // the inverted link must work on a copy, the dispatched value is shared by all registrations and the value store
            var source = await _page.AddNode("source");
            var plain = await _page.AddNode("plain");
            var inverted = await _page.AddNode("inverted");
            _page.LinkNodeToNode(source, inverted, invertTarget: true);
            _page.LinkNodeToNode(source, plain);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true);

            await WaitUntil(() => plain.Written.Count == 1 && inverted.Written.Count == 1);
            Assert.Equal(true, plain.Written.Single());
            Assert.Equal(false, inverted.Written.Single());
            Assert.Equal(true, _page.Dispatcher.GetValue(DispatchableType.NodeInstance, source.Id).Value);
        }

        [Fact]
        public async Task NodeToLogicToNode_PropagatesThroughTheLogic()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            var logic = AddPassThrough("pass");
            logic.Transform = v => (int)v * 2;
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            _page.LinkLogicToNode(Port(logic, "O"), target);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, 21);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(42, target.Written.Single());
            Assert.Equal(21, logic.Received.Single());
            Assert.Equal(42, _page.Dispatcher.GetValue(DispatchableType.RuleInstance, Port(logic, "O").ObjId).Value);
        }

        [Fact]
        public async Task NodeToLogic_InvertedInputPort_InvertsTheValueForTheLogic()
        {
            var source = await _page.AddNode("source");
            var logic = AddPassThrough("pass");
            Port(logic, "I").Inverted = true;
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true);

            await WaitUntil(() => logic.Calls == 1);
            Assert.Equal(false, logic.Received.Single());
            // the source keeps its own value
            Assert.Equal(true, _page.Dispatcher.GetValue(DispatchableType.NodeInstance, source.Id).Value);
        }

        [Fact]
        public async Task Logic_InvertedOutputPort_DispatchesTheInvertedValue()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            var logic = AddPassThrough("pass");
            Port(logic, "O").Inverted = true;
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            _page.LinkLogicToNode(Port(logic, "O"), target);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(false, target.Written.Single());
            Assert.Equal(false, _page.Dispatcher.GetValue(DispatchableType.RuleInstance, Port(logic, "O").ObjId).Value);
        }

        [Fact]
        public async Task LogicToNode_InvertedTarget_InvertsTheLogicOutput()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            var logic = AddPassThrough("pass");
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            _page.LinkLogicToNode(Port(logic, "O"), target, invertTarget: true);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, false);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(true, target.Written.Single());
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, true, true)]
        public async Task LogicToLogic_InvertedPorts_AreAppliedOnce(bool invertOutput, bool invertInput, bool expected)
        {
            var source = await _page.AddNode("source");
            var first = AddPassThrough("first");
            var second = AddPassThrough("second");
            Port(first, "O").Inverted = invertOutput;
            Port(second, "I").Inverted = invertInput;
            _page.LinkNodeToLogic(source, Port(first, "I"));
            _page.LinkLogicToLogic(Port(first, "O"), Port(second, "I"));
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true);

            await WaitUntil(() => second.Calls == 1);
            Assert.Equal(expected, second.Received.Single());
        }

        [Fact]
        public async Task AndGate_OutputsTrue_OnlyWhileBothInputsAreTrue()
        {
            var a = await _page.AddNode("a");
            var b = await _page.AddNode("b");
            var target = await _page.AddNode("target");
            var and = _page.AddLogic("and", ctx => new AndLogic(ctx), new[] { "I1", "I2" }, new[] { "O" });
            _page.LinkNodeToLogic(a, Port(and, "I1"));
            _page.LinkNodeToLogic(b, Port(and, "I2"));
            _page.LinkLogicToNode(Port(and, "O"), target);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(a, true);
            await WaitUntil(() => target.Written.Count == 1);
            await _page.Dispatcher.DispatchValue(b, true);
            await WaitUntil(() => target.Written.Count == 2);
            await _page.Dispatcher.DispatchValue(a, false);
            await WaitUntil(() => target.Written.Count == 3);

            Assert.Equal(new object[] { false, true, false }, target.Written.ToArray());
        }

        [Fact]
        public async Task FanOut_OneLogicOutput_ReachesAllLinkedTargets()
        {
            var source = await _page.AddNode("source");
            var logic = AddPassThrough("pass");
            var targets = new[] { await _page.AddNode("t1"), await _page.AddNode("t2"), await _page.AddNode("t3") };
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            foreach (var t in targets)
            {
                _page.LinkLogicToNode(Port(logic, "O"), t);
            }
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, 5);

            await WaitUntil(() => targets.All(t => t.Written.Count == 1));
            Assert.All(targets, t => Assert.Equal(5, t.Written.Single()));
        }

        [Fact]
        public async Task LogicOutputs_ArriveInDispatchOrder()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            var first = AddPassThrough("first");
            var second = AddPassThrough("second");
            _page.LinkNodeToLogic(source, Port(first, "I"));
            _page.LinkLogicToLogic(Port(first, "O"), Port(second, "I"));
            _page.LinkLogicToNode(Port(second, "O"), target);
            await _page.Engine.Load();

            var expected = Enumerable.Range(1, 30).Cast<object>().ToArray();
            foreach (var i in Enumerable.Range(1, 30))
            {
                await _page.Dispatcher.DispatchValue(source, i);
            }

            await WaitUntil(() => target.Written.Count == 30);
            Assert.Equal(expected, target.Written.ToArray());
        }

        [Fact]
        public async Task IgnoreDuplicateValues_SkipsAnEqualBoxedValue()
        {
            var source = await _page.AddNode("source");
            var logic = AddPassThrough("dedup", ignoreDuplicates: true);
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true);
            await WaitUntil(() => logic.Calls == 1);
            await _page.Dispatcher.DispatchValue(source, true);
            await _page.Dispatcher.DispatchValue(source, false);
            await WaitUntil(() => logic.Calls >= 2);
            await Task.Delay(50);

            Assert.Equal(new object[] { true, false }, logic.Received.ToArray());
        }

        [Fact]
        public async Task WithoutIgnoreDuplicateValues_EveryValueIsProcessed()
        {
            var source = await _page.AddNode("source");
            var logic = AddPassThrough("all");
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true);
            await _page.Dispatcher.DispatchValue(source, true);

            await WaitUntil(() => logic.Calls == 2);
        }

        [Fact]
        public async Task Cycle_BetweenTwoLogics_IsStoppedByTheLoopDetection()
        {
            var source = await _page.AddNode("source");
            var a = AddPassThrough("a");
            var b = AddPassThrough("b");
            _page.LinkNodeToLogic(source, Port(a, "I"));
            _page.LinkLogicToLogic(Port(a, "O"), Port(b, "I"));
            _page.LinkLogicToLogic(Port(b, "O"), Port(a, "I"));
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, 1);

            var calls = await WaitUntilStable(() => a.Calls + b.Calls);
            // every hop (logic output dispatch) counts, the chain is cut at DispatchChain.MaxDepth
            Assert.InRange(calls, 10, 3 * DispatchChain.MaxDepth);
        }

        [Fact]
        public async Task InvertingCycle_Oscillator_IsStoppedAndThePageStillWorks()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            var a = AddPassThrough("a");
            var b = AddPassThrough("b");
            Port(b, "O").Inverted = true;
            _page.LinkNodeToLogic(source, Port(a, "I"));
            _page.LinkLogicToLogic(Port(a, "O"), Port(b, "I"));
            _page.LinkLogicToLogic(Port(b, "O"), Port(a, "I"));
            var other = AddPassThrough("other");
            var otherSource = await _page.AddNode("otherSource");
            _page.LinkNodeToLogic(otherSource, Port(other, "I"));
            _page.LinkLogicToNode(Port(other, "O"), target);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true);
            var calls = await WaitUntilStable(() => a.Calls + b.Calls);
            Assert.InRange(calls, 10, 3 * DispatchChain.MaxDepth);
            // the values alternate, the inverted output flipped them
            Assert.Contains(true, a.Received);
            Assert.Contains(false, a.Received);

            await _page.Dispatcher.DispatchValue(otherSource, "after the loop");
            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal("after the loop", target.Written.Single());
        }

        [Fact]
        public async Task LogicToNodeToLogic_LoopThroughANode_DoesNotSpin()
        {
            // a value written to a node is dispatched with source Write and not forwarded by the node's links
            var node = await _page.AddNode("node");
            var logic = AddPassThrough("pass");
            _page.LinkNodeToLogic(node, Port(logic, "I"));
            _page.LinkLogicToNode(Port(logic, "O"), node);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(node, 1);

            await WaitUntil(() => node.Written.Count == 1);
            await Task.Delay(100);
            Assert.Equal(1, logic.Calls);
        }

        [Fact]
        public async Task Unlink_RemovesOnlyTheGivenLink()
        {
            var source = await _page.AddNode("source");
            var t1 = await _page.AddNode("t1");
            var t2 = await _page.AddNode("t2");
            var l1 = _page.LinkNodeToNode(source, t1);
            _page.LinkNodeToNode(source, t2);
            await _page.Engine.Load();

            await _page.Engine.Unlink(l1.ObjId);
            await _page.Dispatcher.DispatchValue(source, 1);

            await WaitUntil(() => t2.Written.Count == 1);
            await Task.Delay(50);
            Assert.Empty(t1.Written);
        }

        [Fact]
        public async Task Unlink_ByNodeId_RemovesAllLinksFromThatNode()
        {
            var source = await _page.AddNode("source");
            var other = await _page.AddNode("other");
            var t1 = await _page.AddNode("t1");
            var t2 = await _page.AddNode("t2");
            _page.LinkNodeToNode(source, t1);
            _page.LinkNodeToNode(source, t2);
            _page.LinkNodeToNode(other, t2);
            await _page.Engine.Load();

            await _page.Engine.Unlink(source.Id);
            await _page.Dispatcher.DispatchValue(source, 1);
            await _page.Dispatcher.DispatchValue(other, 2);

            await WaitUntil(() => t2.Written.Count == 1);
            await Task.Delay(50);
            Assert.Empty(t1.Written);
            Assert.Equal(2, t2.Written.Single());
        }

        [Fact]
        public async Task UnloadAndLoad_DoesNotDuplicateDeliveries()
        {
            var source = await _page.AddNode("source");
            var logic = AddPassThrough("pass");
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            await _page.Engine.Load();
            await _page.Engine.Unload();
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, 1);

            await WaitUntil(() => logic.Calls >= 1);
            await Task.Delay(100);
            Assert.Equal(1, logic.Calls);
        }

        [Fact]
        public async Task Unload_StopsAllPropagation_ButKeepsForeignSubscriptions()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            _page.LinkNodeToNode(source, target);
            var foreign = 0;
            await _page.Dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, (_, _) =>
            {
                Interlocked.Increment(ref foreign);
                return Task.CompletedTask;
            });
            await _page.Engine.Load();

            await _page.Engine.Unload();
            await _page.Dispatcher.DispatchValue(source, 1);

            await WaitUntil(() => Volatile.Read(ref foreign) == 1);
            await Task.Delay(50);
            Assert.Empty(target.Written);
        }

        [Fact]
        public async Task VisualizationValue_ForALogicInput_ReachesTheLogic()
        {
            var logic = AddPassThrough("pass");
            var target = await _page.AddNode("target");
            _page.LinkLogicToNode(Port(logic, "O"), target);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(new VisuDispatchable(Port(logic, "I").ObjId), 3);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(3, target.Written.Single());
        }

        [Fact]
        public async Task RemanentTarget_StoresTheForwardedValue()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target", remanent: true);
            _page.LinkNodeToNode(source, target, invertTarget: true);
            await _page.Engine.Load();

            await _page.Dispatcher.DispatchValue(source, true);

            await WaitUntil(() => target.Written.Count == 1);
            _page.Remanent.Verify(a => a.SaveValueAsync(target.Id, It.Is<DispatchValue>(v => Equals(v.Value, false)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LinkWithAMissingEnd_IsSkipped_OtherLinksStillWork()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            var logic = AddPassThrough("pass");
            var dangling = AddPassThrough("removed");
            _page.Interfaces.Remove(Port(dangling, "I").ObjId);
            _page.LinkNodeToLogic(source, Port(dangling, "I"));
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            _page.LinkLogicToNode(Port(logic, "O"), target);

            Assert.True(await _page.Engine.Load());
            await _page.Dispatcher.DispatchValue(source, 1);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(0, dangling.Calls);
        }

        [Fact]
        public async Task LogicInterfaceRemovedAfterLoad_IgnoresTheValue()
        {
            var source = await _page.AddNode("source");
            var logic = AddPassThrough("pass");
            var witness = await _page.AddNode("witness");
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            _page.LinkNodeToNode(source, witness);
            await _page.Engine.Load();

            _page.Interfaces.Remove(Port(logic, "I").ObjId);
            await _page.Dispatcher.DispatchValue(source, 1);

            await WaitUntil(() => witness.Written.Count == 1);
            await Task.Delay(50);
            Assert.Equal(0, logic.Calls);
        }

        [Fact]
        public async Task ThrowingLogic_DoesNotStopTheNextValues()
        {
            var source = await _page.AddNode("source");
            var target = await _page.AddNode("target");
            var logic = AddPassThrough("pass");
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            _page.LinkLogicToNode(Port(logic, "O"), target);
            await _page.Engine.Load();

            logic.Throw = true;
            await _page.Dispatcher.DispatchValue(source, 1);
            await WaitUntil(() => logic.Calls == 1);
            logic.Throw = false;
            await _page.Dispatcher.DispatchValue(source, 2);

            await WaitUntil(() => target.Written.Count == 1);
            Assert.Equal(2, target.Written.Single());
        }

        [Fact]
        public async Task LogicOutput_IsSentToTheRemoteClients()
        {
            var source = await _page.AddNode("source");
            var logic = AddPassThrough("pass");
            _page.LinkNodeToLogic(source, Port(logic, "I"));
            await _page.Engine.Load();
            var outputs = await _page.Record(Port(logic, "O"));

            await _page.Dispatcher.DispatchValue(source, 9);

            await WaitUntil(() => outputs.Count == 1);
            _page.RemoteSender.Verify(a => a.DispatchValue(It.Is<IDispatchable>(d => d.Id == Port(logic, "O").ObjId), It.Is<object>(v => v is DispatchValue && Equals(((DispatchValue)v).Value, 9))), Times.Once);
        }
    }
}
