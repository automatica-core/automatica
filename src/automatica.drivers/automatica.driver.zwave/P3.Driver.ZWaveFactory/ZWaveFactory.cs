using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;

namespace P3.Driver.ZWave
{
    public class ZWaveFactory : DriverFactory
    {
        public override string DriverName => "ZWave";

        public override Guid DriverGuid => new Guid("66e4c6b5-c71e-4a39-adcb-63d1e30e121f");
        
        public override Version DriverVersion => new Version(0, 0, 0, 1);

        public override bool InDevelopmentMode => true;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new ZWaveDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "ZWAVE.NAME", "ZWAVE.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "ZWAVE.NAME", "ZWAVE.DESCRIPTION", "zwave", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);
            
            //TODO: Create your node structure here
        }
    }
}
