
        using System;
        using System.Collections.Generic;
        using System.Diagnostics.CodeAnalysis;
        using System.Text;
        using Automatica.Core.Base.Templates;
        using Automatica.Core.EF.Models;
        using P3.Driver.EnOcean.DriverFactory.Templates;
        using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

        namespace P3.Driver.EnOcean.DriverFactory
        {
                [SuppressMessage("ReSharper", "InconsistentNaming")]
                public class EnOceanRorgD5Data : EnOceanRorgData
                {
                    
// AUTO GENERATED 16.11.2018 10:34:17
// -----------------------------------------
// -----------------------------------------
// 0xD5 --------------------------------
// -----------------------------------------
// -----------------------------------------
public static readonly Guid RorgD5Guid = new Guid("eb2c5295-4a34-4389-8bb5-d5ffff000000");
public static readonly Guid FunctionD5_00Guid = new Guid("eb2c5295-4a34-4389-8bb5-d500ff000000");
public static readonly Guid TypeD5_00_01_1Guid = new Guid("eb2c5295-4a34-4389-8bb5-d50001010000");
public static readonly Guid DataFieldD5_00_01_1_CO_Guid = new Guid("eb2c5295-4a34-4389-8bb5-d50001010100");
public static readonly Guid DataFieldD5_00_01_1_LRN_Guid = new Guid("eb2c5295-4a34-4389-8bb5-d50001010200");
        public static void AddRorgD5Templates(INodeTemplateFactory factory, EnOceanTemplateFactory enoceanFactory)
        {
            var nodeGuid = RorgD5Guid;

            Guid interfaceGuid = GenerateNewGuid(nodeGuid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_D5.NAME", "ENOCEAN.RORG_D5.DESCRIPTION",Int32.MaxValue, int.MaxValue, true);

           
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_D5.NAME", "ENOCEAN.RORG_D5.DESCRIPTION",
                "enocean-rorg-D5", EnOceanDriverFactory.InterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xD5, nodeGuid);
            AddFunctionD5_00Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddFunctionD5_00Templates(INodeTemplateFactory factory, Guid rorgInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(FunctionD5_00Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_D5.FUNCTION_00.NAME", "ENOCEAN.RORG_D5.FUNCTION_00.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            factory.CreateNodeTemplate(FunctionD5_00Guid, "ENOCEAN.RORG_D5.FUNCTION_00.NAME", "ENOCEAN.RORG_D5.FUNCTION_00.DESCRIPTION",
                "enocean-function-00", rorgInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xD5, FunctionD5_00Guid);
            AddTypesD5_00_01_1Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddTypesD5_00_01_1Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeD5_00_01_1Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.NAME", "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeD5_00_01_1Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.NAME", "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.DESCRIPTION",
                "enocean-type-00", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xD5, nodeGuid);
            AddDataFieldD5_00_01_1_COTemplates(factory, interfaceGuid);
AddDataFieldD5_00_01_1_LRNTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldD5_00_01_1_COTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldD5_00_01_1_CO_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_CO.NAME", "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_CO.DESCRIPTION",
                "enocean-shortcut-CO", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_COCO.OFFSET", "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_COCO.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "7", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_COCO.LENGTH", "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_COCO.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldD5_00_01_1_LRNTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldD5_00_01_1_LRN_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_LRN.NAME", "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_LRN.DESCRIPTION",
                "enocean-shortcut-LRN", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_LRNLRN.OFFSET", "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_LRNLRN.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "4", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_LRNLRN.LENGTH", "ENOCEAN.RORG_D5.FUNCTION_00.TYPE_01_1.SHORTCUT_LRNLRN.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
                }
        }