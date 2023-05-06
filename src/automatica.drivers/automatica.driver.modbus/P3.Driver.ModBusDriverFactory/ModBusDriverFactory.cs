using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using P3.Driver.ModBusDriver;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;
// ReSharper disable InconsistentNaming

namespace P3.Driver.ModBusDriverFactory
{
    public enum ModBus8ByteOrder
    {
        AB_CD_EF_GH,
        GH_EF_CD_AB,
        BA_DC_FE_HG,
        HF_FE_DC_BA
    }

    public enum ModBus4ByteOrder
    {
        AB_CD,
        CD_AB,
        BA_DC,
        DC_BA
    }
    public abstract class ModBusDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public static readonly Guid AttributeInterface = new Guid("66f1633e-3797-459d-98a0-4bd13d2cd262");

        public static readonly Guid MasterTcpBusInterface = new Guid("806dcb0b-7c3d-4adf-a99f-6c2ffc0f7482");
        public static readonly Guid SlaveTcpBusInterface = new Guid("12947f58-b412-4b7e-8f64-bedc6edc2947");

        public static readonly Guid DeviceInterface = new Guid("7d66b77c-13e9-4529-b4af-1aa67c12e115");

        public static readonly Guid DeviceTemplate = new Guid("d94ca42b-bc52-4cb0-aabc-0013b34d0ff6");

        public static readonly Guid CoilsYesNo = new Guid("8d3daa91-a0be-4dde-a952-6b66eafe9a3c");
        public static readonly Guid Register2ByteGuid = new Guid("ae1a21a0-96a0-4855-b833-65d91a94c5e3");
        public static readonly Guid Register2ByteUnsignedGuid = new Guid("3db442cf-78b8-4b93-abf2-873727b958d3");

        public static readonly Guid Register4ByteGuid = new Guid("4760f07c-bc9f-4734-97f3-b15d0e4d3ab4");
        public static readonly Guid Register4ByteUnsignedGuid = new Guid("d44b0082-8392-4fbb-9606-1587b95a1aeb");

        public static readonly Guid Register8ByteGuid = new Guid("80d29125-95ea-44e2-bce3-57580ae698ad");
        public static readonly Guid Register8ByteUnsignedGuid = new Guid("34734fe8-3f65-4289-ab8c-cc521628288d");

        public static readonly Guid Register4ByteFloat = new Guid("1f591464-45e9-4268-b830-d928e15ec6b4");
        public static readonly Guid Register8ByteFloat = new Guid("231cf740-592d-41a1-a290-b8946dd55760");

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DeviceInterface, "MODBUS.DEVICE", "", 250, 250, true);
            factory.CreateInterfaceType(AttributeInterface, "MODBUS.ATTRIBUTE", "", ushort.MaxValue, byte.MaxValue, false);

            factory.CreateNodeTemplate(DeviceTemplate, "MODBUS.DEVICE.NAME", "MODBUS.DEVICE.DESCRIPTION", "modbus-device",
                DeviceInterface, AttributeInterface, false, true, false, true, false, NodeDataType.NoAttribute, 250, false);

            var devicePropGuid = new Guid("7765b031-ba80-485e-a136-7d6ed4dc7918");
            factory.CreatePropertyTemplate(devicePropGuid, "MODBUS.DEVICE.ID", "", "modbus-device-id",
                PropertyTemplateType.Integer, DeviceTemplate, "MODBUS.CATEGORY.DEVICE", true, false, "", 0, 0, 1);
            
            factory.CreatePropertyConstraint(new Guid("7765b031-ba80-485e-a136-7d6ed4dc79FF"), "MODBUS.CONSTRAINT.UNIQUE_ADDRESS.NAME",
                "MODBUS.CONSTRAINT.UNIQUE_ADDRESS.NAME", PropertyConstraint.Unique, PropertyConstraintLevel.Error, devicePropGuid);

            var pollGuid = new Guid("01021bd7-d4d1-4bee-8cc6-7dfe5e47feb4");
            factory.CreatePropertyTemplate(pollGuid,
                "MODBUS.PROPERTY.POLL_INTERVAL.NAME", "MODBUS.PROPERTY.POLL_INTERVAL.DESCRIPTION", "modbus-poll-interval",
                PropertyTemplateType.Integer, DeviceTemplate, "MODBUS.CATEGORY.DEVICE", true, false,
                PropertyHelper.CreateRangeMetaString(1, 65535), 5000, 1, 1);

            var pollGuidConstraint = new Guid("2e2d2e72-f0ee-4fa5-902d-3cdba4db7949");
            factory.CreatePropertyConstraint(pollGuidConstraint,
                "MODBUS.CONSTRAINT.POLL_INTERVAL_ON_MASTER.NAME",
                "MODBUS.CONSTRAINT.POLL_INTERVAL_ON_MASTER.DESCRIPTION", PropertyConstraint.Visible,
                PropertyConstraintLevel.None, pollGuid);

            factory.CreatePropertyConstraintData(new Guid("d4f060da-583d-4b0c-a75b-f12a3526f2f6"), 1, 0, pollGuidConstraint, "modbus-type",
                PropertyConstraintConditionType.ParentCondition);
            factory.CreatePropertyConstraintData(new Guid("2faa4f9b-ca1d-491f-aea1-b013f6570331"), 1, 0, pollGuidConstraint, ":MODBUS-MASTER",
                PropertyConstraintConditionType.ParentCondition);

            factory.CreateNodeTemplate(Register2ByteGuid, "MODBUS.ATTRIBUTE.INTEGER.POS2BYTE.NAME", "MODBUS.ATTRIBUTE.INTEGER.POS2BYTE.DESCRIPTION", "modbus-+2byte",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, ushort.MaxValue, false);
            CreatePropertiesForModbusValue(Register2ByteGuid, factory, true, 1);

           factory.CreateNodeTemplate(Register2ByteUnsignedGuid, "MODBUS.ATTRIBUTE.INTEGER.NEG2BYTE.NAME", "MODBUS.ATTRIBUTE.INTEGER.NEG2BYTE.DESCRIPTION", "modbus-2byte",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, ushort.MaxValue, false);
            CreatePropertiesForModbusValue(Register2ByteUnsignedGuid, factory, true, 1);

            factory.CreateNodeTemplate(Register4ByteGuid, "MODBUS.ATTRIBUTE.INTEGER.POS4BYTE.NAME", "MODBUS.ATTRIBUTE.INTEGER.POS4BYTE.DESCRIPTION", "modbus-+4byte",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, ushort.MaxValue, false);
            CreatePropertiesForModbusValue(Register4ByteGuid, factory, true, 2);

            factory.CreateNodeTemplate(Register4ByteUnsignedGuid, "MODBUS.ATTRIBUTE.INTEGER.NEG4BYTE.NAME", "MODBUS.ATTRIBUTE.INTEGER.NEG4BYTE.DESCRIPTION", "modbus-4byte",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, short.MaxValue, false);
            CreatePropertiesForModbusValue(Register4ByteUnsignedGuid, factory, true, 2);

            factory.CreateNodeTemplate(Register8ByteGuid, "MODBUS.ATTRIBUTE.INTEGER.POS8BYTE.NAME", "MODBUS.ATTRIBUTE.INTEGER.POS8BYTE.DESCRIPTION", "modbus-+8byte",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, ushort.MaxValue, false);
            CreatePropertiesForModbusValue(Register8ByteGuid, factory, true, 4);

            factory.CreateNodeTemplate(Register8ByteUnsignedGuid, "MODBUS.ATTRIBUTE.INTEGER.NEG8BYTE.NAME", "MODBUS.ATTRIBUTE.INTEGER.NEG8BYTE.DESCRIPTION", "modbus-8byte",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, ushort.MaxValue, false);
            CreatePropertiesForModbusValue(Register8ByteUnsignedGuid, factory, true, 4);

            factory.CreateNodeTemplate(Register4ByteFloat, "MODBUS.ATTRIBUTE.FLOAT.4BYTE.NAME", "MODBUS.ATTRIBUTE.FLOAT.4BYTE.DESCRIPTION", "modbus-4float",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Double, ushort.MaxValue, false);
            CreatePropertiesForModbusValue(Register4ByteFloat, factory, true, 2);

            factory.CreateNodeTemplate(Register8ByteFloat, "MODBUS.ATTRIBUTE.FLOAT.8BYTE.NAME", "MODBUS.ATTRIBUTE.FLOAT.8BYTE.DESCRIPTION", "modbus-8float",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Double, ushort.MaxValue, false);
            CreatePropertiesForModbusValue(Register8ByteFloat, factory, true, 4);

            factory.CreateNodeTemplate(CoilsYesNo, "MODBUS.ATTRIBUTE.BINARY.NAME", "MODBUS.ATTRIBUTE.BINARY.DESCRIPTION", "modbus-binary",
                AttributeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Boolean, ushort.MaxValue, false);
            CreatePropertiesForModbusValue(CoilsYesNo, factory, false, 1);



            InitTemplates(factory);
        }

        private void CreatePropertiesForModbusValue(Guid nodeGuid, INodeTemplateFactory factory, bool isByteRegister, int registerLength)
        {
            var propGuid = GenerateNewGuid(nodeGuid, 1);
            factory.CreatePropertyTemplate(propGuid, "MODBUS.PROPERTY.REGISTER", "",
                "modbus-register", PropertyTemplateType.Range, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false,
                PropertyHelper.CreateRangeMetaString(0, ushort.MaxValue - 1), 0, 0, 0);

            var propConstraint = GenerateNewGuid(nodeGuid, 10);
            factory.CreatePropertyConstraint(propConstraint,
                "MODBUS.CONSTRAINT.REGISTER_ADDRESS.NAME", "MODBUS.CONSTRAINT.REGISTER_ADDRESS.DESCRIPTION",
                PropertyConstraint.UniqueOnCondition, PropertyConstraintLevel.Warn, propGuid);

            var modbusTablePropGuid = GenerateNewGuid(nodeGuid, 2);
            var modbusRegLengthPropGuid = GenerateNewGuid(nodeGuid, 6);

            factory.CreatePropertyConstraintData(GenerateNewGuid(nodeGuid, 20), 1, -1, propConstraint, "modbus-table",
                PropertyConstraintConditionType.Unique);
            factory.CreatePropertyConstraintData(GenerateNewGuid(nodeGuid, 30), 1, -1, propConstraint, "modbus-register-length",
                PropertyConstraintConditionType.UniqueRange);

            if (isByteRegister)
            {
                factory.CreatePropertyTemplate(modbusTablePropGuid, "MODBUS.PROPERTY.TABLE", "", "modbus-table",
                    PropertyTemplateType.Enum, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false,
                    PropertyHelper.CreateEnumMetaString(typeof(ModBusTable), 0, 1), 0, 0, 0);

                factory.ChangeDefaultVisuTemplate(nodeGuid, VisuMobileObjectTemplateTypes.NumberBox);
                if (registerLength == 2)
                {
                    factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 3), "MODBUS.PROPERTY.BYTEORDER", "", "modbus-byte-order",
                        PropertyTemplateType.Enum, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false,
                        PropertyHelper.CreateEnumMetaString(typeof(ModBus4ByteOrder)), 0, 0, 0);
                }
                else if (registerLength == 4)
                {
                    factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 3), "MODBUS.PROPERTY.BYTEORDER", "", "modbus-byte-order",
                        PropertyTemplateType.Enum, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false,
                        PropertyHelper.CreateEnumMetaString(typeof(ModBus8ByteOrder)), 0, 0,
                        0);
                }

                factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 4), "COMMON.FACTOR.NAME", "COMMON.FACTOR.DESCRIPTION", "factor",
                    PropertyTemplateType.Numeric, nodeGuid, "COMMON.CATEGORY.TRANSFORM", true, false, "", 1, 2, 1);
                factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 5), "COMMON.OFFSET.NAME", "COMMON.OFFSET.DESCRIPTION", "offset",
                    PropertyTemplateType.Numeric, nodeGuid, "COMMON.CATEGORY.TRANSFORM", true, false, "", 0, 2, 2);

                factory.CreatePropertyTemplate(modbusRegLengthPropGuid, "MODBUS.REGISTER.LENGTH", "MODBUS.REGISTER.LENGTH", "modbus-register-length",
                    PropertyTemplateType.Numeric, nodeGuid, "COMMON.CATEGORY.TRANSFORM", false, true, "", registerLength, 2, 2);
            }
            else
            {
                factory.ChangeDefaultVisuTemplate(nodeGuid, VisuMobileObjectTemplateTypes.ToggleButton);
                factory.CreatePropertyTemplate(modbusTablePropGuid, "MODBUS.PROPERTY.TABLE", "", "modbus-table",
                    PropertyTemplateType.Enum, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false,
                    PropertyHelper.CreateEnumMetaString(typeof(ModBusTable), 2, 3), 2, 0, 0);

                factory.CreatePropertyTemplate(modbusRegLengthPropGuid, "MODBUS.REGISTER.LENGTH", "MODBUS.REGISTER.LENGTH", "modbus-register-length",
                    PropertyTemplateType.Numeric, nodeGuid, "COMMON.CATEGORY.TRANSFORM", false, true, "", 1, 2, 2);
            }

        }

        private Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[^1] = (byte)(Convert.ToInt32(gu[^1]) + c);

            return new Guid(gu);
        }


  

        public abstract void InitTemplates(INodeTemplateFactory factory);

    }
}
