using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Templates;
using Automatica.Core.Control.Cache;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.Core.Plugins.Logics;
using Automatica.Core.Runtime.IO;
using Automatica.Core.Tests.Dispatcher.Utils;
using Automatica.Core.Tests.Model;
using Automatica.Core.UnitTests.Base.Logics;
using Automatica.Core.UnitTests.Drivers;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CoreDispatcher = Automatica.Core.Base.IO.Dispatcher;

namespace Automatica.Core.Tests.LogicPages
{
    /// <summary>
    /// A logic page built in code: the real <see cref="CoreDispatcher"/> and <see cref="LogicEngineDispatcher"/>, logic
    /// instances, driver nodes and the links between them
    /// </summary>
    internal sealed class LogicPageHarness : IDisposable
    {
        public IDispatcher Dispatcher { get; }
        public LinkCacheMock Links { get; } = new();
        public NodeInstanceCacheMock Nodes { get; } = new();
        public LogicInterfaceInstanceCacheMock Interfaces { get; } = new();
        public LogicNodeInstanceCacheMock LogicNodes { get; } = new();
        public LogicInstanceCacheMock LogicInstances { get; } = new();
        public LogicInstanceStore LogicStore { get; }
        public DriverNodesStore DriverNodes { get; } = new();
        public Mock<IRemanentHandler> Remanent { get; } = new();
        public Mock<IRemoteSender> RemoteSender { get; } = new();
        public Mock<IDataBroadcast> Broadcast { get; } = new();
        public LogicEngineDispatcher Engine { get; }

        public LogicPageHarness()
        {
            Remanent.Setup(a => a.SaveValueAsync(It.IsAny<Guid>(), It.IsAny<DispatchValue>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            Dispatcher = new CoreDispatcher(NullLogger<CoreDispatcher>.Instance, Broadcast.Object, RemoteSender.Object, Remanent.Object, new LogicInstanceVisuNotifyMock());
            LogicStore = new LogicInstanceStore(LogicInstances, new Mock<IControlCache>().Object);
            Engine = new LogicEngineDispatcher(Links, Dispatcher, LogicStore, DriverNodes, Nodes, Interfaces,
                NullLogger<LogicEngineDispatcher>.Instance, new LogicInstanceVisuNotifyMock(), Remanent.Object, LogicNodes);
        }

        public async Task<RecordingDriverNode> AddNode(string name, bool remanent = false)
        {
            var parent = new NodeInstance { ObjId = Guid.NewGuid(), Name = name + "Parent" };
            var child = new NodeInstance { ObjId = Guid.NewGuid(), Name = name, IsRemanent = remanent, This2ParentNodeInstanceNavigation = parent };
            parent.InverseThis2ParentNodeInstanceNavigation.Add(child);
            Nodes.Add(parent.ObjId, parent);
            Nodes.Add(child.ObjId, child);

            var driver = new RecordingDriverNode(new DriverContextMock(parent, new DriverFactoryMock(), new NodeTemplateFactoryMock(), Dispatcher,
                new NullLoggerFactory(), DateTimeHelper.ProviderInstance));
            await driver.Configure();
            await driver.StartInternal();

            var node = (RecordingDriverNode)driver.Children[0];
            DriverNodes.Add(driver.Id, driver);
            DriverNodes.Add(node.Id, node);
            return node;
        }

        /// <summary>
        /// A logic instance with the given inputs and outputs (interface names), all linkable
        /// </summary>
        public T AddLogic<T>(string name, Func<ILogicContext, T> create, string[] inputs, string[] outputs) where T : Automatica.Core.Logic.Logic
        {
            var rule = new RuleInstance { ObjId = Guid.NewGuid(), Name = name };

            void AddPort(string port, LogicInterfaceDirection direction)
            {
                var template = new RuleInterfaceTemplate
                {
                    ObjId = Guid.NewGuid(),
                    Name = port,
                    Key = port,
                    This2RuleInterfaceDirection = (long)direction,
                    ParameterDataType = RuleInterfaceParameterDataType.Bool,
                    IsLinkableParameter = true
                };
                rule.RuleInterfaceInstance.Add(new RuleInterfaceInstance
                {
                    ObjId = Guid.NewGuid(),
                    This2RuleInstance = rule.ObjId,
                    This2RuleInstanceNavigation = rule,
                    This2RuleInterfaceTemplate = template.ObjId,
                    This2RuleInterfaceTemplateNavigation = template
                });
            }

            foreach (var input in inputs)
            {
                AddPort(input, LogicInterfaceDirection.Input);
            }
            foreach (var output in outputs)
            {
                AddPort(output, LogicInterfaceDirection.Output);
            }

            var logic = create(new LogicContextMock(rule, new LogicTemplateFactoryMock(), Dispatcher));
            logic.Start().GetAwaiter().GetResult();

            LogicInstances.Add(rule.ObjId, rule);
            foreach (var port in rule.RuleInterfaceInstance)
            {
                Interfaces.Add(port.ObjId, port);
            }
            LogicStore.Add(rule, logic);
            return logic;
        }

        public static RuleInterfaceInstance Port(Automatica.Core.Logic.Logic logic, string name)
        {
            return logic.Context.RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplateNavigation.Name == name);
        }

        private NodeInstance2RulePage OnPage(IDriverNode node, bool inverted)
        {
            var onPage = new NodeInstance2RulePage { ObjId = Guid.NewGuid(), This2NodeInstance = node.Id, Inverted = inverted };
            LogicNodes.AddOrUpdate(onPage.ObjId, onPage);
            return onPage;
        }

        private Link AddLink(Action<Link> setup)
        {
            var link = new Link { ObjId = Guid.NewGuid() };
            setup(link);
            Links.Add(link.ObjId, link);
            return link;
        }

        public Link LinkNodeToNode(IDriverNode source, IDriverNode target, bool invertTarget = false) => AddLink(a =>
        {
            a.This2NodeInstance2RulePageOutputNavigation = OnPage(source, false);
            a.This2NodeInstance2RulePageInputNavigation = OnPage(target, invertTarget);
        });

        public Link LinkNodeToLogic(IDriverNode source, RuleInterfaceInstance input) => AddLink(a =>
        {
            a.This2NodeInstance2RulePageOutputNavigation = OnPage(source, false);
            a.This2RuleInterfaceInstanceInputNavigation = input;
        });

        public Link LinkLogicToLogic(RuleInterfaceInstance output, RuleInterfaceInstance input) => AddLink(a =>
        {
            a.This2RuleInterfaceInstanceOutputNavigation = output;
            a.This2RuleInterfaceInstanceInputNavigation = input;
        });

        public Link LinkLogicToNode(RuleInterfaceInstance output, IDriverNode target, bool invertTarget = false) => AddLink(a =>
        {
            a.This2RuleInterfaceInstanceOutputNavigation = output;
            a.This2NodeInstance2RulePageInputNavigation = OnPage(target, invertTarget);
        });

        /// <summary>
        /// Records every value dispatched by the logic interface <paramref name="port"/>
        /// </summary>
        public async Task<ConcurrentQueue<object>> Record(RuleInterfaceInstance port)
        {
            var values = new ConcurrentQueue<object>();
            await Dispatcher.Subscribe(DispatchableType.RuleInstance, port.ObjId, (_, v) =>
            {
                values.Enqueue(v.Value);
                return Task.CompletedTask;
            });
            return values;
        }

        public static async Task WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var start = Environment.TickCount64;
            while (!condition())
            {
                if (Environment.TickCount64 - start > timeoutMs)
                {
                    throw new TimeoutException("condition not reached");
                }
                await Task.Delay(10);
            }
        }

        /// <summary>
        /// Waits until <paramref name="count"/> stops changing for <paramref name="quietMs"/>
        /// </summary>
        public static async Task<int> WaitUntilStable(Func<int> count, int quietMs = 150, int timeoutMs = 10000)
        {
            var start = Environment.TickCount64;
            var last = count();
            var lastChange = Environment.TickCount64;
            while (Environment.TickCount64 - start < timeoutMs)
            {
                await Task.Delay(10);
                var now = count();
                if (now != last)
                {
                    last = now;
                    lastChange = Environment.TickCount64;
                }
                else if (Environment.TickCount64 - lastChange >= quietMs)
                {
                    return now;
                }
            }
            throw new TimeoutException("value did not settle");
        }

        public void Dispose()
        {
            Engine.Unload().GetAwaiter().GetResult();
            Dispatcher.ClearRegistrations().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// A driver node that records the values written to it (and does not dispatch them back)
    /// </summary>
    public class RecordingDriverNode : DriverBase
    {
        public ConcurrentQueue<object> Written { get; } = new();

        public RecordingDriverNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            DriverContext.NodeInstance.State = NodeInstanceState.InUse;
            return base.Start(token);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            Written.Enqueue(value);
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new RecordingDriverNode(ctx);
        }
    }

    /// <summary>
    /// Input I to output O
    /// </summary>
    internal class PassThroughLogic : Automatica.Core.Logic.Logic
    {
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public ConcurrentQueue<object> Received { get; } = new();
        public Func<object, object> Transform { get; set; } = v => v;
        public bool Throw { get; set; }
        private readonly bool _ignoreDuplicates;

        public PassThroughLogic(ILogicContext context, bool ignoreDuplicates = false) : base(context)
        {
            _ignoreDuplicates = ignoreDuplicates;
        }

        public override bool IgnoreDuplicateValues => _ignoreDuplicates;

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            Interlocked.Increment(ref _calls);
            Received.Enqueue(value);
            if (Throw)
            {
                throw new InvalidOperationException("logic failed");
            }
            return new List<ILogicOutputChanged> { new LogicOutputChanged(LogicPageHarness.Port(this, "O"), Transform(value)) };
        }
    }

    /// <summary>
    /// O = I1 AND I2
    /// </summary>
    internal class AndLogic : Automatica.Core.Logic.Logic
    {
        private bool _i1, _i2;

        public AndLogic(ILogicContext context) : base(context)
        {
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var b = Convert.ToBoolean(value);
            if (instance.This2RuleInterfaceTemplateNavigation.Name == "I1")
            {
                _i1 = b;
            }
            else
            {
                _i2 = b;
            }
            return new List<ILogicOutputChanged> { new LogicOutputChanged(LogicPageHarness.Port(this, "O"), _i1 && _i2) };
        }
    }
}
