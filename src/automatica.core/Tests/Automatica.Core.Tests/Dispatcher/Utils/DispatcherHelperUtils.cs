using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Drivers;

namespace Automatica.Core.Tests.Dispatcher.Utils
{
    public class DriverNodeMock : DriverBase
    {
        public bool WriteReceived { get; set; }
        public DriverNodeMock(IDriverContext driverContext) : base(driverContext)
        {
        }


        public override Task WriteValue(IDispatchable source, object value)
        {
            WriteReceived = true;

            DispatchValue(value);

            return Task.CompletedTask;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new DriverNodeMock(ctx);
        }
    }
    public class LogicMock : Core.Rule.Rule
    {
        public bool WriteReceived { get; set; }
        public RuleInterfaceInstance Output { get; set; }
        public LogicMock(IRuleContext ruleContext) : base(ruleContext)
        {
            Output = ruleContext.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplateNavigation.Name == "Output");
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            WriteReceived = true;

            return new List<IRuleOutputChanged>() {new RuleOutputChanged(Output, true)};
        }
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
            var mock = new DriverNodeMock(new DriverContextMock(mockNode, new NodeTemplateFactoryMock(), dispatcher));

            mock.Configure();
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

            var mock = new LogicMock(new RuleContextMock(ruleInstance, dispatcher));

            await mock.Start();

            instanceCache?.Add(ruleInstance.ObjId, ruleInstance);
            interfaceInstanceCache?.Add(input.ObjId, input);
            interfaceInstanceCache?.Add(output.ObjId, output);
            logicInstances?.Add(ruleInstance, mock);

            return mock;
        }


    }
}
