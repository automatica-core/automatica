using System;
using System.Collections.Generic;
using System.Reflection;
using Automatica.Core.Base.Templates;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices.SG04LP3;
using Xunit;

namespace P3.Driver.ModBus.Tests.Solarman
{
    public class SolarmanTemplateTests
    {
        /// <summary>
        /// The core treats a node whose interface type is a driver interface as a driver root: adding such a group starts it as
        /// a driver of its own ("No factory found") instead of reloading the Solarman driver. Only the device may be one.
        /// </summary>
        [Fact]
        public void OnlyTheDeviceIsADriverInterface()
        {
            var factory = new SG04LP3DriverFactory();
            var templates = DispatchProxy.Create<INodeTemplateFactory, RecordingTemplateFactory>();

            factory.InitNodeTemplates(templates);

            var interfaces = ((RecordingTemplateFactory)(object)templates).InterfaceTypes;
            Assert.Equal(8, interfaces.Count);
            Assert.True(interfaces[factory.DeviceTypeGuid]);
            Assert.All(interfaces, entry => Assert.Equal(entry.Key == factory.DeviceTypeGuid, entry.Value));
        }

        public class RecordingTemplateFactory : DispatchProxy
        {
            public Dictionary<Guid, bool> InterfaceTypes { get; } = new();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == nameof(INodeTemplateFactory.CreateInterfaceType))
                {
                    InterfaceTypes[(Guid)args[0]] = (bool)args[5];
                }

                var returnType = targetMethod.ReturnType;
                return returnType.IsValueType && returnType != typeof(void) ? Activator.CreateInstance(returnType) : null;
            }
        }
    }
}
