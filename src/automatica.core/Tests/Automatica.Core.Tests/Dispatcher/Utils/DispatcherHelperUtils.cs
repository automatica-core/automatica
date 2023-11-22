using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Base.Logics;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.Tests.Dispatcher.Utils
{
    public class DriverNodeMock : DriverBase
    {
        public bool WriteReceived { get; set; }
        public DriverNodeMock(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            WriteReceived = true;
            writeContext.DispatchValue(value, token);
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext writeContext, CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new DriverNodeMock(ctx);
        }
    }
    public class LogicMock : Logic.Logic
    {
        public bool WriteReceived { get; set; }
        public RuleInterfaceInstance Output { get; set; }
        public LogicMock(ILogicContext logicContext) : base(logicContext)
        {
            Output = logicContext.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplateNavigation.Name == "Output");
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            WriteReceived = true;

            return new List<ILogicOutputChanged>() {new LogicOutputChanged(Output, true)};
        }
    }

    public class DriverFactoryMock : IDriverFactory
    {
        public void InitTemplates(INodeTemplateFactory factory)
        {
        }

        public Guid FactoryGuid => Guid.NewGuid();
        public string DriverName => "Mock";
        public Guid DriverGuid => Guid.NewGuid();
        public Version DriverVersion => new Version(0, 0, 0, 1);
        public InterfaceTypeEnum[] UsesInterfaces => new[] { InterfaceTypeEnum.Board };
        public bool InDevelopmentMode => true;
        public void InitNodeTemplates(INodeTemplateFactory factory)
        {
        }

        public IDriver CreateDriver(IDriverContext config)
        {
            return null;
        }

        public void Scan(NodeInstance instance)
        {

        }

        public string ImageSource => "";
        public string ImageName => "";
        public string Tag  => "";
    }

    internal static class DispatcherHelperUtils
    {
        public static async Task<DriverNodeMock> CreateNodeMock(Guid guid, string name, IDispatcher dispatcher, INodeInstanceCache cache=null, IDriverNodesStore store = null)
        {
            var mockNode = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Name = name + "Parent"
            };
            var mockNodeChild = new NodeInstance
            {
                ObjId = guid,
                Name = name
            };

            cache?.Add(mockNode.ObjId, mockNode);
            cache?.Add(mockNodeChild.ObjId, mockNodeChild);

            mockNode.InverseThis2ParentNodeInstanceNavigation.Add(mockNodeChild);
            var mock = new DriverNodeMock(new DriverContextMock(mockNode, new DriverFactoryMock(), new NodeTemplateFactoryMock(), dispatcher, new NullLoggerFactory()));

            await mock.Configure();
            await mock.Start();

            store?.Add(mock.Id, mock);
            store?.Add(mock.Children[0].Id, mock.Children[0]);

            return mock;
        }

        public static async Task<LogicMock> CreateLogicMock(
            string name, 
            IDispatcher dispatcher,
            ILogicInstanceCache instanceCache, 
            ILogicInterfaceInstanceCache interfaceInstanceCache,
            ILogicInstancesStore logicInstances)
        {
            var ruleInstance = new RuleInstance
            {
                ObjId = Guid.NewGuid(),
                Name = "Logic_" + name
            };

            var inputTemplate = new RuleInterfaceTemplate
            {
                ObjId = Guid.NewGuid(),
                Name = "Input",
                ParameterDataType = RuleInterfaceParameterDataType.Integer,
                IsLinkableParameter = true
            };

            var input = new RuleInterfaceInstance
            {
                ObjId = Guid.NewGuid(),
                This2RuleInstanceNavigation = ruleInstance,
                This2RuleInstance = ruleInstance.ObjId,
                This2RuleInterfaceTemplateNavigation = inputTemplate,
                This2RuleInterfaceTemplate = inputTemplate.ObjId
            };


            var outputTemplate = new RuleInterfaceTemplate
            {
                ObjId = Guid.NewGuid(),
                Name = "Output",
                ParameterDataType = RuleInterfaceParameterDataType.Integer,
                IsLinkableParameter = true
            };
            var output = new RuleInterfaceInstance
            {
                ObjId = Guid.NewGuid(),
                This2RuleInstanceNavigation = ruleInstance,
                This2RuleInstance = ruleInstance.ObjId,
                This2RuleInterfaceTemplateNavigation = outputTemplate,
                This2RuleInterfaceTemplate = outputTemplate.ObjId
            };

            ruleInstance.RuleInterfaceInstance.Add(input);
            ruleInstance.RuleInterfaceInstance.Add(output);

            var mock = new LogicMock(new LogicContextMock(ruleInstance, new LogicTemplateFactoryMock(), dispatcher));

            await mock.Start();

            instanceCache?.Add(ruleInstance.ObjId, ruleInstance);
            interfaceInstanceCache?.Add(input.ObjId, input);
            interfaceInstanceCache?.Add(output.ObjId, output);
            logicInstances?.Add(ruleInstance, mock);

            return mock;
        }


    }
}
