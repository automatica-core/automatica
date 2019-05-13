using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Constants
{
    public class ConstantsDriverFactory : DriverFactory
    {

        public static readonly Guid InterfaceId = new Guid("5926638a-e9f8-48cb-8401-c8042170ff1b");

        public static readonly Guid BusId = new Guid("2ba2fdfe-3df0-4986-80c1-d0f695d64fdc");
        public static readonly Guid ValueId = new Guid("d46b8d4d-29e6-45bd-ba62-9463692bcbd7");

        public static readonly Guid PiId = new Guid("36a0da8a-2735-4f83-91ef-9af90262de32");
        public static readonly Guid HalfPiId = new Guid("bde14ed8-24b3-476b-9c8a-751da617a50b");
        public static readonly Guid DoublePiId = new Guid("82e579a7-935e-463b-9d26-c75b31113553");


        public static readonly Guid PropertyValueId = new Guid("5d5c0c1f-50e2-4946-94c0-842cfc51a478");

        public override string DriverName => "consts";
        public override Guid DriverGuid => BusId;
        public override Version DriverVersion => new Version(0, 1, 0, 1);

        public override string ImageName => "automaticacore/plugin-p3.driver.constants";

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(InterfaceId, "CONSTANTS.NAME", "CONSTANTS.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(BusId, "CONSTANTS.NAME", "CONSTANTS.DESCRIPTION", "consts", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                InterfaceId, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreateNodeTemplate(ValueId, "CONSTANTS.NODE.CONSTANT.NAME", "CONSTANTS.NODE.CONSTANT.DESCRIPTION", "const", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Integer, Int32.MaxValue, false);
            factory.CreatePropertyTemplate(PropertyValueId, "CONSTANTS.PROPERTY.VALUE.NAME", "CONSTANTS.PROPERTY.VALUE.DESCRIPTION", "const_value", PropertyTemplateType.Integer,
                ValueId, "CONSTANTS.CATEGORY.VALUE", true, false, "", "", 1, 1);


            factory.CreateNodeTemplate(PiId, "CONSTANTS.NODE.PI.NAME", "CONSTANTS.NODE.PI.DESCRIPTION", "const_pi", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, Int32.MaxValue, false);

            factory.CreateNodeTemplate(HalfPiId, "CONSTANTS.NODE.HALF_PI.NAME", "CONSTANTS.NODE.HALF_PI.DESCRIPTION", "const_halfpi", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, Int32.MaxValue, false);


            factory.CreateNodeTemplate(DoublePiId, "CONSTANTS.NODE.DOUBLE_PI.NAME", "CONSTANTS.NODE.DOUBLE_PI.DESCRIPTION", "const_doublepi", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, Int32.MaxValue, false);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new ConstantsDriver(config);
        }

        public override void AfterSave(NodeInstance instance)
        {
            // do nothing
        }
    }
}
