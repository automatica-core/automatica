using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.MachineFlags
{
    public class MachineFlagsDriverFactory : DriverFactory
    {

        public static readonly Guid InterfaceId = new Guid("b6d06629-2449-4b77-879c-9ef68a327637");

        public static readonly Guid BusId = new Guid("cb1c7119-6d3a-4085-8094-a85f63d11dd1");
        public static readonly Guid ValueId = new Guid("9fa0dbb7-4603-44bb-9699-89910fa36858");

        

        public override string DriverName => "machine-flags";
        public override Guid DriverGuid => BusId;
        public override Version DriverVersion => new Version(0, 1, 0, 2);

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(InterfaceId, "MACHINE_FLAGS.NAME", "MACHINE_FLAGS.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(BusId, "MACHINE_FLAGS.NAME", "MACHINE_FLAGS.DESCRIPTION", "machine-flags", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                InterfaceId, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreateNodeTemplate(ValueId, "MACHINE_FLAGS.NODE.NAME", "MACHINE_FLAGS.NODE.DESCRIPTION", "machine-flag", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false, NodeDataType.Double, Int32.MaxValue, false);
            factory.ChangeDefaultVisuTemplate(ValueId, VisuMobileObjectTemplateTypes.NumberBox);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new MachineFlagsDriver(config);
        }
    }
}
