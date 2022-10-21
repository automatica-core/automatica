using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;

namespace P3.Driver.EBusFactory
{
    public class EBusFactory : DriverFactory
    {
        public override string DriverName => "ebus";

        public override Guid DriverGuid => new Guid("15dc0233-fdee-4420-b037-781b13b02cfc");
        
        public override Version DriverVersion => new Version(0, 0, 0, 2);

        public override bool InDevelopmentMode => false;

        public override string ImageName => "automaticacore/plugin-p3.driver.ebus";

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new EBusDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "EBUS.NAME", "EBUS.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "EBUS.NAME", "EBUS.DESCRIPTION", "EBUS", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);
            
            //TODO: Create your node structure here
        }
    }
}
