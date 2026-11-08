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

            Assert.Equal(6, parameters.Count);
            Assert.All(parameters, c => Assert.Contains(c.Method.GetParameters(), p => p.Name == "linkable"));
            var keys = parameters.Select(c => (string)c.Args[Array.FindIndex(c.Method.GetParameters(), p => p.Name == "key")]).ToList();
            Assert.Equal(new[] { "invertGrid", "invertBattery", "peakPower", "consumer1Name", "consumer2Name", "consumer3Name" }, keys);
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
