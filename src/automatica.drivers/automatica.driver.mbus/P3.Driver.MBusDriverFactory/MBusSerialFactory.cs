using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;

namespace P3.Driver.MBusDriverFactory
{
    public class MBusSerialFactory : DriverFactory
    {
        public override Version DriverVersion => new Version(0, 1, 0, 0);

        public override string DriverName => "MBus";
        public override Guid DriverGuid => MBusUdpFactory.MbusSerialNode;

        public override string ImageName => "automaticacore/plugin-p3.driver.mbus";
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
           
        }
        public override IDriver CreateDriver(IDriverContext config)
        {
            return new Driver(config, MBusType.Serial);
        }
    }
}
