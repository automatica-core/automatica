using System;
using System.Diagnostics.CodeAnalysis;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.EnOcean.DriverFactory.Driver;
using P3.Driver.EnOcean.DriverFactory.Templates;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.EnOcean.DriverFactory
{
    public class EnOceanDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public static readonly Guid InterfaceGuid = new Guid("eb2c5295-4a34-4389-8bb5-29eddeb67404");
        public static readonly Guid DriverGuidId = new Guid("f90bec2c-2152-4b97-8ad3-d5b3ea9a6ff4");
        public static readonly Guid LearnedGuid = new Guid("2d3b5c51-056d-4c86-97fe-383fb19e69a7");
        public static readonly Guid SimulatedGuid = new Guid("1d6e9cd1-5e95-4ddf-9d36-b86f93d12151");

        private readonly EnOceanTemplateFactory _enoceanFactory = new EnOceanTemplateFactory();

        public override string DriverName => "EnOcean";
        public override Guid DriverGuid => DriverGuidId;
        public override Version DriverVersion => new Version(0, 1, 0, 10);

        public override string ImageName => "automaticacore/plugin-p3.driver.enocean";
        public override string Tag => "latest-develop";

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            var gwguid = new Guid("2ca34b3f-1c29-4ab1-99d2-ecfbf2683dd5"); 
            factory.CreateInterfaceType(gwguid, "ENOCEAN.DEVICE.NAME", "ENOCEAN.DEVICE.DESCRIPTION", int.MaxValue, 1,
                true);
            factory.CreateInterfaceType(InterfaceGuid, "ENOCEAN.LEARNED.NAME", "ENOCEAN.LEARNED.DESCRIPTION", int.MaxValue, 1,
                true);
            factory.CreateInterfaceType(SimulatedGuid, "ENOCEAN.SIMULATED.NAME", "ENOCEAN.SIMULATED.DESCRIPTION", int.MaxValue, 1,
                true);

            factory.CreateNodeTemplate(DriverGuid, "ENOCEAN.DEVICE.NAME", "ENOCEAN.DEVICE.DESCRIPTION",
                "enocean-device", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb), gwguid, false, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(new Guid("f68e5b59-3fea-4739-a7c5-a2e2b52c08ff"), "ENOCEAN.DEVICE.PORT.NAME",
                "ENOCEAN.DEVICE.PORT.DESCRIPTION", "enocean-port", PropertyTemplateType.UsbPort, DriverGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);

            factory.CreateNodeTemplate(LearnedGuid, "ENOCEAN.LEARNED.NAME", "ENOCEAN.LEARNED.DESCRIPTION",
                "enocean-learned", gwguid, InterfaceGuid, true, false, true, false, true, NodeDataType.NoAttribute, 1,
                false);
            factory.CreatePropertyTemplate(new Guid("409ae6c0-3e65-4d7b-9ace-120d2074eec7"), "ENOCEAN.LEARN.NAME", "ENOCEAN.LEARN.DESCRIPTION", "learn", PropertyTemplateType.LearnMode, LearnedGuid, "COMMON.CATEGORY.MISC", true, false, null, null, 1, 1);

            factory.CreateNodeTemplate(SimulatedGuid, "ENOCEAN.SIMULATED.NAME", "ENOCEAN.SIMULATED.DESCRIPTION",
                "enocean-simulated", gwguid, SimulatedGuid, true, false, true, false, true, NodeDataType.NoAttribute, 1,
                false);
            
            EnOceanRorgD5Data.AddRorgD5Templates(factory, _enoceanFactory);
            EnOceanRorgA5Data.AddRorgA5Templates(factory, _enoceanFactory);
            EnOceanRorgD2Data.AddRorgD2Templates(factory, _enoceanFactory);
            EnOceanRorgF6Data.AddRorgF6Templates(factory, _enoceanFactory);

            AddWriteableDatapoints(factory);
        }

        private void AddWriteableDatapoints(INodeTemplateFactory factory)
        {
            var simulatedRelayGuid = new Guid("4a0c8c59-e8c5-4098-8420-15e886f4648f");
            factory.CreateInterfaceType(simulatedRelayGuid, "ENOCEAN.SIMULATED.RELAY.NAME", "ENOCEAN.SIMULATED.RELAY.DESCRIPTION", 1, Int32.MaxValue, false);

            factory.CreateNodeTemplate(new Guid("6cffa073-afcf-4cd6-8364-d294f45a17e3"), "ENOCEAN.SIMULATED.RELAY.NAME", "ENOCEAN.SIMULATED.RELAY.DESCRIPTION", "enocean-simulated-relay", SimulatedGuid, simulatedRelayGuid,
                false, false, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);


            factory.CreateNodeTemplate(new Guid("ac3fb6ce-88dd-4385-8e30-9b5a6be9b593"), "ENOCEAN.SIMULATED.RELAY.VALUE.NAME", "ENOCEAN.SIMULATED.RELAY.VALUE.DESCRIPTION", "enocean-simulated-relay-value", simulatedRelayGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value),
                true, false, true, true, true, NodeDataType.Boolean, 1, false);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            Logger.Logger.Instance = config.Logger;
            return new EnOceanDriver(config, _enoceanFactory);
        }

      

    }
}
