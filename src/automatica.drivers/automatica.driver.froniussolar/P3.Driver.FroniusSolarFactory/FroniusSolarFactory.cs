using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;

namespace P3.Driver.FroniusSolarFactory
{
    public class FroniusSolarFactory : DriverFactory
    {
        public override string DriverName => "FroniusSolar";

        public override Guid DriverGuid => new Guid("c0491f87-83e4-4510-bad2-e21ebbc490d1");
        
        public override Version DriverVersion => new Version(0, 0, 0, 2);

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new FroniusSolarDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "FRONIUS_SOLAR.NAME", "FRONIUS_SOLAR.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "FRONIUS_SOLAR.NAME", "FRONIUS_SOLAR.DESCRIPTION", "fronius_solar", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);
            
            //TODO: Create your node structure here
        }
    }
}
