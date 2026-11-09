using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Automatica.Core.Base.Templates;
using Automatica.Logic.Metering.EnergyMonitor;
using Xunit;

namespace Automatica.Logic.Metering.Tests.EnergyMonitor
{
    public class EnergyMonitorLogicFactoryTests
    {
        /// <summary>
        /// The energy flow tile reads peakPower and consumerNName by template key. The core's
        /// CreateParameterLogicInterfaceTemplate overload with key but without "linkable" stores the name as key,
        /// so every parameter has to go through an overload that takes "linkable" (the shared factory mock
        /// honours the key in all overloads and would not catch this).
        /// </summary>
        [Fact]
        public void Parameters_UseTheOverloadThatKeepsTheKey()
        {
            var calls = RecordingFactory.Record(new EnergyMonitorLogicFactory());

            var parameters = calls.Where(c => c.Method.Name == nameof(ILogicTemplateFactory.CreateParameterLogicInterfaceTemplate)).ToList();

            Assert.Equal(9, parameters.Count);
            Assert.All(parameters, c => Assert.Contains(c.Method.GetParameters(), p => p.Name == "linkable"));
            var keys = parameters.Select(c => (string)c.Args[Array.FindIndex(c.Method.GetParameters(), p => p.Name == "key")]).ToList();
            Assert.Equal(new[] { "invertGrid", "invertBattery", "peakPower", "consumer1Name", "consumer2Name", "consumer3Name",
                "consumer4Name", "consumer5Name", "consumer6Name" }, keys);
        }

        /// <summary>
        /// Existing logic instances keep their links through the template ids, so consumers 1-3 must keep theirs;
        /// consumers 4-6 came with 1.1.
        /// </summary>
        [Fact]
        public void Consumers_KeepTheirTemplateIds()
        {
            Assert.Equal(EnergyFlowInputs.ConsumerCount, EnergyMonitorLogicFactory.InputConsumers.Length);
            Assert.Equal(EnergyFlowInputs.ConsumerCount, EnergyMonitorLogicFactory.OutputConsumers.Length);
            Assert.Equal(EnergyFlowInputs.ConsumerCount, EnergyMonitorLogicFactory.ParamConsumerNames.Length);

            Assert.Equal(new[] { new Guid("38443425-7feb-4931-a731-bebb737f375b"), new Guid("7322bfe1-3f09-4884-95f4-958a4cb35c11"), new Guid("9d0c6855-3d20-4522-b36f-6a77b29cd8e1") },
                EnergyMonitorLogicFactory.InputConsumers.Take(3));
            Assert.Equal(new[] { new Guid("01ce23af-f2a7-4589-b8c2-2c4add4f5d40"), new Guid("9582934c-fb92-4ee3-90a4-a2f4952c4c79"), new Guid("b11212c4-1c5e-4426-87a1-ebf960e43249") },
                EnergyMonitorLogicFactory.OutputConsumers.Take(3));
            Assert.Equal(new[] { new Guid("c84e1ab6-010f-4dab-8fc2-d8f7646df031"), new Guid("56aa2d0a-ea2d-43f9-b821-be4ae8f6eeb1"), new Guid("39847236-d148-434f-a1f0-7ae438649253") },
                EnergyMonitorLogicFactory.ParamConsumerNames.Take(3));
        }

        [Fact]
        public void TemplateIds_AreUnique()
        {
            var calls = RecordingFactory.Record(new EnergyMonitorLogicFactory());

            var ids = calls.Where(c => c.Method.Name is nameof(ILogicTemplateFactory.CreateLogicInterfaceTemplate)
                    or nameof(ILogicTemplateFactory.CreateParameterLogicInterfaceTemplate))
                .Select(c => (Guid)c.Args[0])
                .ToList();

            Assert.Equal(17 + 22 + 9, ids.Count);
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        /// <summary>the core only adds new interfaces to existing instances when the logic version goes up</summary>
        [Fact]
        public void Version_WasRaisedForConsumers4To6()
        {
            Assert.True(new EnergyMonitorLogicFactory().LogicVersion >= new Version(1, 1, 0, 0));
        }

        public class RecordingFactory : DispatchProxy
        {
            public List<(MethodInfo Method, object[] Args)> Calls { get; } = new();

            public static List<(MethodInfo Method, object[] Args)> Record(Automatica.Core.Logic.LogicFactory logicFactory)
            {
                var proxy = Create<ILogicTemplateFactory, RecordingFactory>();
                logicFactory.InitTemplates(proxy);
                return ((RecordingFactory)(object)proxy).Calls;
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                Calls.Add((targetMethod, args));
                var type = targetMethod.ReturnType;
                return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
            }
        }
    }
}
