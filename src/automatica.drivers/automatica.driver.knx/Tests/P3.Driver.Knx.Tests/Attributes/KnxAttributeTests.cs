using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Knx.Falcon;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Knx.DriverFactory.Attributes;
using P3.Driver.Knx.DriverFactory.Factories.Baos;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using Xunit;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Knx.Tests.Attributes
{
    /// <summary>
    /// Bus values in both directions for the built-in datapoint types (DPT 2, 3, 6, 7, 10, 13) and the BAOS interface.
    /// </summary>
    public class KnxAttributeTests : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        private const string MiddleGroup = "1/2";

        internal sealed class RecordingKnxDriver(IDriverContext driverContext) : KnxDriver(driverContext, false)
        {
            public List<(string Address, GroupValue Value)> Writes { get; } = new();
            public List<string> Reads { get; } = new();

            public override Task<bool> Read(string address)
            {
                Reads.Add(address);
                return Task.FromResult(true);
            }

            public override Task<bool> Write(KnxGroupAddress source, string address, GroupValue groupValue, CancellationToken token)
            {
                Writes.Add((address, groupValue));
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Counts the values dispatched per node.
        /// </summary>
        public class CountingDispatcher : DispatchProxy
        {
            public List<(Guid Id, object Value)> Dispatched { get; } = new();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == nameof(IDispatcher.DispatchValue) && args[0] is IDispatchable self)
                {
                    Dispatched.Add((self.Id, args[1] is DispatchValue dv ? dv.Value : args[1]));
                }

                if (targetMethod.ReturnType == typeof(Task))
                {
                    return Task.CompletedTask;
                }

                return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
            }
        }

        private sealed class WriteContextStub : IWriteContext
        {
            public List<object> Values { get; } = new();

            public Task DispatchValue(object value, CancellationToken token = default)
            {
                Values.Add(value);
                return Task.CompletedTask;
            }
        }

        private NodeInstance CreateByKey(string key, int? address = null)
        {
            var node = Factory.CreateNodeInstanceByKey(key);
            Assert.True(node != null, key);
            node.ObjId = Guid.NewGuid();
            node.IsReadable = true;
            node.IsWriteable = true;
            if (address.HasValue)
            {
                node.SetProperty("knx-address", address.Value);
            }

            return node;
        }

        /// <summary>
        /// Gateway with one main group 1 and middle group 2, the given nodes are added below the middle group.
        /// </summary>
        private Task<(RecordingKnxDriver Driver, KnxMiddleGroup Middle)> CreateDriver(params NodeInstance[] groupAddresses)
        {
            return CreateDriver(Dispatcher, groupAddresses);
        }

        private async Task<(RecordingKnxDriver Driver, KnxMiddleGroup Middle)> CreateDriver(IDispatcher dispatcher, params NodeInstance[] groupAddresses)
        {
            var gateway = CreateNodeInstance(KnxIpDriverFactory.KnxGateway);
            gateway.SetProperty("knx-ip", "127.0.0.1");

            var threeLevel = CreateNodeInstance(KnxIpDriverFactory.KnxIpGateway3LevelInterface);
            var main = CreateNodeInstance(KnxIpDriverFactory.KnxIp3LevelMainAddress);
            main.SetProperty("knx-address", 1);
            var middle = CreateNodeInstance(KnxIpDriverFactory.KnxIp3LevelMiddleAddress);
            middle.SetProperty("knx-address", 2);

            gateway.InverseThis2ParentNodeInstanceNavigation.Add(threeLevel);
            threeLevel.InverseThis2ParentNodeInstanceNavigation.Add(main);
            main.InverseThis2ParentNodeInstanceNavigation.Add(middle);
            foreach (var ga in groupAddresses)
            {
                middle.InverseThis2ParentNodeInstanceNavigation.Add(ga);
            }

            var context = new DriverContextMock(gateway, DriverFactory, Factory, dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = new RecordingKnxDriver(context);
            Assert.True(await driver.Configure());

            var middleNode = (KnxMiddleGroup)driver.Children.Single().Children.Single().Children.Single();
            Assert.Equal(groupAddresses.Length, middleNode.Children.Count);
            return (driver, middleNode);
        }

        private static T Node<T>(KnxMiddleGroup middle, NodeInstance node) where T : class, IDriverNode
        {
            return (T)middle.Children.Single(a => a.Id == node.ObjId);
        }

        private static IDriverNode Child(IDriverNode parent, NodeInstance node)
        {
            return parent.Children.Single(a => a.Id == node.ObjId);
        }

        private async Task<object> WaitForDispatch(NodeInstance node)
        {
            for (var i = 0; i < 100; i++)
            {
                var value = Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId);
                if (value != null)
                {
                    return value.Value;
                }

                await Task.Delay(20);
            }

            throw new TimeoutException($"{node.Name} did not dispatch a value");
        }

        private static readonly MethodInfo WriteMethod = typeof(DriverBase).GetMethod("Write", BindingFlags.Instance | BindingFlags.NonPublic,
            new[] { typeof(object), typeof(IWriteContext), typeof(CancellationToken) })!;

        private static Task Write(IDriverNode node, object value, IWriteContext context = null)
        {
            return (Task)WriteMethod.Invoke(node, new[] { value, context ?? new WriteContextStub(), CancellationToken.None })!;
        }

        private static string Hex(GroupValue value) => BitConverter.ToString(value.Value);

        [Fact]
        public async Task Dpt7UsesUnsignedCountInsteadOfTimePeriod()
        {
            var dpt7 = CreateByKey("knx-dpt7", 3);
            var (driver, middle) = await CreateDriver(dpt7);

            var ga = Node<KnxDpt7Attribute>(middle, dpt7);
            Assert.Equal(1, ga.DptSubType);

            driver.GroupValueReceived($"{MiddleGroup}/3", new GroupValue(new byte[] { 0x01, 0x02 }));

            var value = await WaitForDispatch(dpt7);
            Assert.IsNotType<TimeSpan>(value);
            Assert.Equal(258, Convert.ToInt32(value));

            await Write(ga, 500);
            Assert.Equal("01-F4", Hex(driver.Writes.Single().Value));
        }

        [Fact]
        public async Task ExplicitSubTypeIsStillUsed()
        {
            var dpt5 = CreateByKey("knx-dpt5", 3);
            dpt5.SetProperty("knx-dpt-sub", 10);
            var (_, middle) = await CreateDriver(dpt5);

            Assert.Equal(10, Node<KnxDpt5Attribute>(middle, dpt5).DptSubType);
        }

        [Fact]
        public async Task Dpt10DispatchesTimeOfDay()
        {
            var dpt10 = CreateByKey("knx-dpt10", 4);
            var (driver, _) = await CreateDriver(dpt10);

            driver.GroupValueReceived($"{MiddleGroup}/4", new GroupValue(new byte[] { 0x2C, 0x1E, 0x05 })); // Monday 12:30:05

            Assert.Equal(new TimeSpan(12, 30, 5), await WaitForDispatch(dpt10));
        }

        [Fact]
        public async Task Dpt13IsAnIntegerNode()
        {
            var dpt13 = CreateByKey("knx-dpt13", 5);
            Assert.Equal(new Guid("94d9744e-4677-4d60-b7bc-0638cc355675"), dpt13.This2NodeTemplate);
            Assert.Equal((long)NodeDataType.Integer, dpt13.This2NodeTemplateNavigation.This2NodeDataType);

            var (driver, middle) = await CreateDriver(dpt13);

            driver.GroupValueReceived($"{MiddleGroup}/5", new GroupValue(new byte[] { 0xFF, 0xFF, 0xFF, 0xFE }));
            Assert.Equal(-2, await WaitForDispatch(dpt13));

            await Write(Node<KnxDpt13Attribute>(middle, dpt13), 500L); // logic values are not always int
            Assert.Equal("00-00-01-F4", Hex(driver.Writes.Single().Value));
        }

        [Fact]
        public void Dpt3TemplateHasDpt3()
        {
            var dpt3 = CreateByKey("knx-dpt3");
            Assert.Equal(new Guid("99d97125-b566-4cac-8210-3ba792db3c04"), dpt3.This2NodeTemplate);
            Assert.Equal(3, Convert.ToInt32(dpt3.GetProperty("knx-dpt").This2PropertyTemplateNavigation.DefaultValue));
        }

        [Fact]
        public async Task Dpt2ChildrenReceiveBusValues()
        {
            var dpt2 = CreateByKey("knx-dpt2", 6);
            var control = CreateByKey("knx-dpt2-control");
            var value = CreateByKey("knx-dpt2-value");
            dpt2.InverseThis2ParentNodeInstanceNavigation.Add(control);
            dpt2.InverseThis2ParentNodeInstanceNavigation.Add(value);

            var (driver, middle) = await CreateDriver(dpt2);
            var parent = Node<KnxDpt2Attribute>(middle, dpt2);

            driver.GroupValueReceived($"{MiddleGroup}/6", new GroupValue(0b10, 2)); // control = 1, value = 0

            Assert.Equal(true, await WaitForDispatch(control));
            Assert.Equal(false, await WaitForDispatch(value));

            await Child(parent, value).Read();
            Assert.Equal($"{MiddleGroup}/6", driver.Reads.Single());

            var context = new WriteContextStub();
            await Write(Child(parent, value), true, context);
            Assert.Equal(new GroupValue(0b11, 2), driver.Writes.Single().Value); // the new value is sent, not the old one
            Assert.Equal(true, context.Values.Single());
        }

        [Fact]
        public async Task Dpt3ChildrenReceiveBusValuesAndWrite()
        {
            var dpt3 = CreateByKey("knx-dpt3", 7);
            var control = CreateByKey("knx-dpt3-control");
            var stepCode = CreateByKey("knx-dpt3-step-code");
            dpt3.InverseThis2ParentNodeInstanceNavigation.Add(control);
            dpt3.InverseThis2ParentNodeInstanceNavigation.Add(stepCode);

            var (driver, middle) = await CreateDriver(dpt3);
            var parent = Node<KnxDpt3Attribute>(middle, dpt3);
            Assert.Equal(2, parent.Children.Count);

            driver.GroupValueReceived($"{MiddleGroup}/7", new GroupValue(0x0B, 4)); // increase, step code 3

            Assert.Equal(true, await WaitForDispatch(control));
            Assert.Equal(3, await WaitForDispatch(stepCode));

            await Child(parent, stepCode).Read();
            Assert.Equal($"{MiddleGroup}/7", driver.Reads.Single());

            await Write(Child(parent, control), false);
            await Write(Child(parent, stepCode), 5);
            await Write(Child(parent, stepCode), 9); // out of range, ignored

            Assert.Equal(new[] { new GroupValue(0x03, 4), new GroupValue(0x05, 4) }, driver.Writes.Select(a => a.Value));
        }

        [Fact]
        public async Task Dpt6WritesSignedValues()
        {
            var dpt6 = CreateByKey("knx-dpt6", 8);
            var (driver, middle) = await CreateDriver(dpt6);
            var ga = Node<KnxDpt6Attribute>(middle, dpt6);

            Assert.Equal(6, ga.ImplementationDptType);

            await Write(ga, -5);
            await Write(ga, "12");
            await Write(ga, 200); // out of range, ignored

            Assert.Equal(new[] { "FB", "0C" }, driver.Writes.Select(a => Hex(a.Value)));

            driver.GroupValueReceived($"{MiddleGroup}/8", new GroupValue(new byte[] { 0xF6 }));
            Assert.Equal(-10, Convert.ToInt32(await WaitForDispatch(dpt6)));
        }

        [Fact]
        public async Task BaosDatapointsReachTheGroupAddressNodes()
        {
            var baos = new BaosKnxFactory(); // the templates are shared by all KNX factories and already created

            var driverNode = CreateNodeInstance(KnxIpDriverFactory.KnxBaos);
            var datapoints = CreateNodeInstance(new Guid("86db8e85-0770-47a0-9fa9-71558113a0db"));
            var dpt1 = CreateByKey("knx-dpt1", 5);
            var dpt9 = CreateByKey("knx-dpt9", 6);
            var dpt12 = CreateByKey("knx-dpt12", 7); // catalogue codec

            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(datapoints);
            datapoints.InverseThis2ParentNodeInstanceNavigation.Add(dpt1);
            datapoints.InverseThis2ParentNodeInstanceNavigation.Add(dpt9);
            datapoints.InverseThis2ParentNodeInstanceNavigation.Add(dpt12);

            var context = new DriverContextMock(driverNode, baos, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = (KnxBaosDriver)baos.CreateDriver(context);

            Assert.True(await driver.Init()); // no KNXnet/IP settings needed
            Assert.True(await driver.Configure());

            await driver.DataPointInd(new[]
            {
                new P3.Knx.Core.Baos.Driver.Data.DatapointValue { DatapointId = 5, Data = new byte[] { 0x01 } },
                new P3.Knx.Core.Baos.Driver.Data.DatapointValue { DatapointId = 6, Data = new byte[] { 0x0C, 0x1A } },
                new P3.Knx.Core.Baos.Driver.Data.DatapointValue { DatapointId = 7, Data = new byte[] { 0x00, 0x00, 0x01, 0x00 } },
            });

            Assert.Equal(true, await WaitForDispatch(dpt1));
            Assert.Equal(21f, Convert.ToSingle(await WaitForDispatch(dpt9)));
            Assert.Equal(256L, Convert.ToInt64(await WaitForDispatch(dpt12)));
        }

        [Fact]
        public void Dpt6020EncodingFollowsTheStandard()
        {
            // the valid range is "all set and mode 0" to "all cleared and mode 2"
            Assert.Equal(0x01, KnxDpt6020Attribute.Encode(new[] { true, true, true, true, true }, 0));
            Assert.Equal(0xFC, KnxDpt6020Attribute.Encode(new[] { false, false, false, false, false }, 2));
            Assert.Equal(0x5A, KnxDpt6020Attribute.Encode(new[] { true, false, true, false, false }, 1));

            var (flags, mode) = KnxDpt6020Attribute.Decode(0x5A);
            Assert.Equal(new[] { true, false, true, false, false }, flags);
            Assert.Equal(1, mode);
            Assert.Null(KnxDpt6020Attribute.Decode(0x03).Mode); // two modes active is not valid
        }

        [Fact]
        public async Task Dpt6020ChildrenReceiveBusValuesAndWrite()
        {
            var dpt6020 = CreateByKey("knx-dpt6.020", 9);
            var children = new[] { "A", "B", "C", "D", "E", "ACTIVE_MODE" }.Select(a => CreateByKey($"knx-dpt6.020.{a}")).ToArray();
            foreach (var child in children)
            {
                dpt6020.InverseThis2ParentNodeInstanceNavigation.Add(child);
            }

            var (driver, middle) = await CreateDriver(dpt6020);
            var parent = Node<KnxDpt6020Attribute>(middle, dpt6020);
            Assert.Equal(6, parent.Children.Count);

            driver.GroupValueReceived($"{MiddleGroup}/9", new GroupValue(new byte[] { 0x5A })); // A + C set, mode 1

            Assert.Equal(true, await WaitForDispatch(children[0]));
            Assert.Equal(false, await WaitForDispatch(children[1]));
            Assert.Equal(true, await WaitForDispatch(children[2]));
            Assert.Equal(false, await WaitForDispatch(children[3]));
            Assert.Equal(false, await WaitForDispatch(children[4]));
            Assert.Equal(1, await WaitForDispatch(children[5]));

            await Child(parent, children[1]).Read();
            Assert.Equal($"{MiddleGroup}/9", driver.Reads.Single());

            await Write(Child(parent, children[5]), 2);
            await Write(Child(parent, children[1]), true);
            await Write(Child(parent, children[5]), 3); // out of range, ignored

            Assert.Equal(new[] { "5C", "1C" }, driver.Writes.Select(a => Hex(a.Value)));
        }

        [Fact]
        public async Task Dpt1IsDispatchedOnce()
        {
            var proxy = DispatchProxy.Create<IDispatcher, CountingDispatcher>();
            var dpt1 = CreateByKey("knx-dpt1", 10);
            var (driver, _) = await CreateDriver(proxy, dpt1);

            driver.GroupValueReceived($"{MiddleGroup}/10", new GroupValue(true));

            var dispatched = ((CountingDispatcher)(object)proxy).Dispatched.Where(a => a.Id == dpt1.ObjId).ToList();
            Assert.Single(dispatched);
            Assert.Equal(true, dispatched[0].Value);
        }
    }
}
