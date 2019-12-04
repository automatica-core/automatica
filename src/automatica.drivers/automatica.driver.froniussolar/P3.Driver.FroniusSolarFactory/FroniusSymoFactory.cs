using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;

namespace P3.Driver.FroniusSymoFactory
{
    public class FroniusSymoFactory : DriverFactory
    {
        public override string DriverName => "FroniusSymo";

        public override Guid DriverGuid => new Guid("c0491f87-83e4-4510-bad2-e21ebbc490d1");
        
        public override Version DriverVersion => new Version(0, 0, 0, 1);

        public override bool InDevelopmentMode => true;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new FroniusSymoDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "FRONIUSSYMO.NAME", "FRONIUSSYMO.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "FRONIUSSYMO.NAME", "FRONIUSSYMO.DESCRIPTION", "froniussymo", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);
            
            //TODO: Create your node structure here
        }
    }
}
