using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.MachineFlags
{
    public class MachineFlagsDriverFactory : DriverFactory
    {

        public static readonly Guid InterfaceId = new Guid("b6d06629-2449-4b77-879c-9ef68a327637");

        public static readonly Guid BusId = new Guid("cb1c7119-6d3a-4085-8094-a85f63d11dd1");
        public static readonly Guid ValueId = new Guid("9fa0dbb7-4603-44bb-9699-89910fa36858");
        
        public static readonly Guid BinaryId = new Guid("a7498db4-d199-4d29-a05f-235a29bf879e");
        public static readonly Guid NumberId = new Guid("e2b9f0cc-9ff1-4a2d-8ac2-9be01acb981d");
        public static readonly Guid StringId = new Guid("d10ac895-a747-4d9f-b2fa-6841b6329305");
        

        public override string ImageName => "automaticacore/plugin-p3.driver.machine-flags";

        public override string DriverName => "machine-flags";
        public override Guid DriverGuid => BusId;
        public override Version DriverVersion => new Version(1, 0, 0, 0);

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(InterfaceId, "MACHINE_FLAGS.NAME", "MACHINE_FLAGS.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(BusId, "MACHINE_FLAGS.NAME", "MACHINE_FLAGS.DESCRIPTION", "machine-flags", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                InterfaceId, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreateNodeTemplate(ValueId, "MACHINE_FLAGS.NODE.NAME", "MACHINE_FLAGS.NODE.DESCRIPTION", "machine-flag", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false, NodeDataType.Double, Int32.MaxValue, false);
            factory.ChangeDefaultVisuTemplate(ValueId, VisuMobileObjectTemplateTypes.Label);

            factory.CreateNodeTemplate(BinaryId, "MACHINE_FLAGS.BINARY.NAME", "MACHINE_FLAGS.BINARY.DESCRIPTION", "machine-flag-binary", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false, NodeDataType.Boolean, Int32.MaxValue, false);
            factory.ChangeDefaultVisuTemplate(BinaryId, VisuMobileObjectTemplateTypes.ToggleButton);

            factory.CreateNodeTemplate(NumberId, "MACHINE_FLAGS.NUMBER.NAME", "MACHINE_FLAGS.NUMBER.DESCRIPTION", "machine-flag-number", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false, NodeDataType.Double, Int32.MaxValue, false);
            factory.ChangeDefaultVisuTemplate(NumberId, VisuMobileObjectTemplateTypes.NumberBox);


            factory.CreateNodeTemplate(StringId, "MACHINE_FLAGS.STRING.NAME", "MACHINE_FLAGS.STRING.DESCRIPTION", "machine-flag-string", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false, NodeDataType.String, Int32.MaxValue, false);
            factory.ChangeDefaultVisuTemplate(StringId, VisuMobileObjectTemplateTypes.Label);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new MachineFlagsDriver(config);
        }
    }
}
