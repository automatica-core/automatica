
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
                public class EnOceanRorgF6Data : EnOceanRorgData
                {
                    
// AUTO GENERATED 16.11.2018 10:34:17
// -----------------------------------------
// -----------------------------------------
// 0xF6 --------------------------------
// -----------------------------------------
// -----------------------------------------
public static readonly Guid RorgF6Guid = new Guid("eb2c5295-4a34-4389-8bb5-f6ffff000000");
public static readonly Guid FunctionF6_01Guid = new Guid("eb2c5295-4a34-4389-8bb5-f601ff000000");
public static readonly Guid TypeF6_01_01_1Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60101010000");
public static readonly Guid DataFieldF6_01_01_1_PB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60101010100");
public static readonly Guid FunctionF6_02Guid = new Guid("eb2c5295-4a34-4389-8bb5-f602ff000000");
public static readonly Guid TypeF6_02_01_1Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60201010000");
public static readonly Guid DataFieldF6_02_01_1_R1_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60201010100");
public static readonly Guid DataFieldF6_02_01_1_EB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60201010200");
public static readonly Guid DataFieldF6_02_01_1_R2_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60201010300");
public static readonly Guid DataFieldF6_02_01_1_SA_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60201010400");
public static readonly Guid TypeF6_02_01_2Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60201020000");
public static readonly Guid DataFieldF6_02_01_2_R1_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60201020100");
public static readonly Guid DataFieldF6_02_01_2_EB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60201020200");
public static readonly Guid TypeF6_02_02_3Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60202030000");
public static readonly Guid DataFieldF6_02_02_3_R1_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60202030100");
public static readonly Guid DataFieldF6_02_02_3_EB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60202030200");
public static readonly Guid DataFieldF6_02_02_3_R2_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60202030300");
public static readonly Guid DataFieldF6_02_02_3_SA_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60202030400");
public static readonly Guid TypeF6_02_02_4Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60202040000");
public static readonly Guid DataFieldF6_02_02_4_R1_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60202040100");
public static readonly Guid DataFieldF6_02_02_4_EB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60202040200");
public static readonly Guid TypeF6_02_03_5Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60203050000");
public static readonly Guid DataFieldF6_02_03_5_RA_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60203050100");
public static readonly Guid TypeF6_02_04_6Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60204060000");
public static readonly Guid DataFieldF6_02_04_6_EBO_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60204060100");
public static readonly Guid DataFieldF6_02_04_6_BC_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60204060200");
public static readonly Guid DataFieldF6_02_04_6_RBI_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60204060300");
public static readonly Guid DataFieldF6_02_04_6_RB0_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60204060400");
public static readonly Guid DataFieldF6_02_04_6_RAI_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60204060500");
public static readonly Guid DataFieldF6_02_04_6_RA0_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60204060600");
public static readonly Guid FunctionF6_03Guid = new Guid("eb2c5295-4a34-4389-8bb5-f603ff000000");
public static readonly Guid TypeF6_03_01_1Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60301010000");
public static readonly Guid DataFieldF6_03_01_1_R1_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60301010100");
public static readonly Guid DataFieldF6_03_01_1_EB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60301010200");
public static readonly Guid DataFieldF6_03_01_1_R2_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60301010300");
public static readonly Guid DataFieldF6_03_01_1_SA_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60301010400");
public static readonly Guid TypeF6_03_01_2Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60301020000");
public static readonly Guid DataFieldF6_03_01_2_R1_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60301020100");
public static readonly Guid DataFieldF6_03_01_2_EB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60301020200");
public static readonly Guid TypeF6_03_02_3Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60302030000");
public static readonly Guid DataFieldF6_03_02_3_R1_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60302030100");
public static readonly Guid DataFieldF6_03_02_3_EB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60302030200");
public static readonly Guid DataFieldF6_03_02_3_R2_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60302030300");
public static readonly Guid DataFieldF6_03_02_3_SA_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60302030400");
public static readonly Guid TypeF6_03_02_4Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60302040000");
public static readonly Guid DataFieldF6_03_02_4_R1_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60302040100");
public static readonly Guid DataFieldF6_03_02_4_EB_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60302040200");
public static readonly Guid FunctionF6_04Guid = new Guid("eb2c5295-4a34-4389-8bb5-f604ff000000");
public static readonly Guid TypeF6_04_01_1Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60401010000");
public static readonly Guid DataFieldF6_04_01_1_KC_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60401010100");
public static readonly Guid TypeF6_04_01_2Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60401020000");
public static readonly Guid DataFieldF6_04_01_2_KC_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60401020100");
public static readonly Guid TypeF6_04_02_3Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60402030000");
public static readonly Guid DataFieldF6_04_02_3_EBO_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60402030100");
public static readonly Guid DataFieldF6_04_02_3_BC_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60402030200");
public static readonly Guid DataFieldF6_04_02_3_SOC_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60402030300");
public static readonly Guid FunctionF6_05Guid = new Guid("eb2c5295-4a34-4389-8bb5-f605ff000000");
public static readonly Guid TypeF6_05_00_1Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60500010000");
public static readonly Guid DataFieldF6_05_00_1_WND_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60500010100");
public static readonly Guid TypeF6_05_01_2Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60501020000");
public static readonly Guid DataFieldF6_05_01_2_WAS_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60501020100");
public static readonly Guid TypeF6_05_02_3Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60502030000");
public static readonly Guid DataFieldF6_05_02_3_SMO_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f60502030100");
public static readonly Guid FunctionF6_10Guid = new Guid("eb2c5295-4a34-4389-8bb5-f610ff000000");
public static readonly Guid TypeF6_10_00_1Guid = new Guid("eb2c5295-4a34-4389-8bb5-f61000010000");
public static readonly Guid DataFieldF6_10_00_1_WIN_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f61000010100");
public static readonly Guid TypeF6_10_01_2Guid = new Guid("eb2c5295-4a34-4389-8bb5-f61001020000");
public static readonly Guid DataFieldF6_10_01_2_HC_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f61001020100");
public static readonly Guid DataFieldF6_10_01_2_HVL_Guid = new Guid("eb2c5295-4a34-4389-8bb5-f61001020200");
        public static void AddRorgF6Templates(INodeTemplateFactory factory, EnOceanTemplateFactory enoceanFactory)
        {
            var nodeGuid = RorgF6Guid;

            Guid interfaceGuid = GenerateNewGuid(nodeGuid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.NAME", "ENOCEAN.RORG_F6.DESCRIPTION",Int32.MaxValue, int.MaxValue, true);

           
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.NAME", "ENOCEAN.RORG_F6.DESCRIPTION",
                "enocean-rorg-F6", EnOceanDriverFactory.InterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddFunctionF6_01Templates(factory, interfaceGuid, enoceanFactory);
AddFunctionF6_02Templates(factory, interfaceGuid, enoceanFactory);
AddFunctionF6_03Templates(factory, interfaceGuid, enoceanFactory);
AddFunctionF6_04Templates(factory, interfaceGuid, enoceanFactory);
AddFunctionF6_05Templates(factory, interfaceGuid, enoceanFactory);
AddFunctionF6_10Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddFunctionF6_01Templates(INodeTemplateFactory factory, Guid rorgInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(FunctionF6_01Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_01.NAME", "ENOCEAN.RORG_F6.FUNCTION_01.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            factory.CreateNodeTemplate(FunctionF6_01Guid, "ENOCEAN.RORG_F6.FUNCTION_01.NAME", "ENOCEAN.RORG_F6.FUNCTION_01.DESCRIPTION",
                "enocean-function-01", rorgInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xF6, FunctionF6_01Guid);
            AddTypesF6_01_01_1Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddTypesF6_01_01_1Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_01_01_1Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_01_01_1Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.DESCRIPTION",
                "enocean-type-01", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_01_01_1_PBTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_01_01_1_PBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_01_01_1_PB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.SHORTCUT_PB.NAME", "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.SHORTCUT_PB.DESCRIPTION",
                "enocean-shortcut-PB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.SHORTCUT_PBPB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.SHORTCUT_PBPB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.SHORTCUT_PBPB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_01.TYPE_01_1.SHORTCUT_PBPB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddFunctionF6_02Templates(INodeTemplateFactory factory, Guid rorgInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(FunctionF6_02Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_02.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            factory.CreateNodeTemplate(FunctionF6_02Guid, "ENOCEAN.RORG_F6.FUNCTION_02.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.DESCRIPTION",
                "enocean-function-02", rorgInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xF6, FunctionF6_02Guid);
            AddTypesF6_02_01_1Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_02_01_2Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_02_02_3Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_02_02_4Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_02_03_5Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_02_04_6Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddTypesF6_02_01_1Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_02_01_1Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_02_01_1Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.DESCRIPTION",
                "enocean-type-02", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_02_01_1_R1Templates(factory, interfaceGuid);
AddDataFieldF6_02_01_1_EBTemplates(factory, interfaceGuid);
AddDataFieldF6_02_01_1_R2Templates(factory, interfaceGuid);
AddDataFieldF6_02_01_1_SATemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_02_01_1_R1Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_01_1_R1_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R1.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R1.DESCRIPTION",
                "enocean-shortcut-R1", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R1R1.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R1R1.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R1R1.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R1R1.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_01_1_EBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_01_1_EB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_EB.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_EB.DESCRIPTION",
                "enocean-shortcut-EB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_EBEB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_EBEB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_EBEB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_EBEB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_01_1_R2Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_01_1_R2_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R2.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R2.DESCRIPTION",
                "enocean-shortcut-R2", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R2R2.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R2R2.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "4", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R2R2.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_R2R2.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_01_1_SATemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_01_1_SA_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_SA.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_SA.DESCRIPTION",
                "enocean-shortcut-SA", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_SASA.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_SASA.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "7", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_SASA.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_1.SHORTCUT_SASA.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddTypesF6_02_01_2Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_02_01_2Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_02_01_2Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.DESCRIPTION",
                "enocean-type-02", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_02_01_2_R1Templates(factory, interfaceGuid);
AddDataFieldF6_02_01_2_EBTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_02_01_2_R1Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_01_2_R1_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_R1.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_R1.DESCRIPTION",
                "enocean-shortcut-R1", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_R1R1.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_R1R1.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_R1R1.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_R1R1.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_01_2_EBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_01_2_EB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_EB.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_EB.DESCRIPTION",
                "enocean-shortcut-EB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_EBEB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_EBEB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_EBEB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_01_2.SHORTCUT_EBEB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddTypesF6_02_02_3Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_02_02_3Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_02_02_3Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.DESCRIPTION",
                "enocean-type-02", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_02_02_3_R1Templates(factory, interfaceGuid);
AddDataFieldF6_02_02_3_EBTemplates(factory, interfaceGuid);
AddDataFieldF6_02_02_3_R2Templates(factory, interfaceGuid);
AddDataFieldF6_02_02_3_SATemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_02_02_3_R1Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_02_3_R1_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R1.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R1.DESCRIPTION",
                "enocean-shortcut-R1", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R1R1.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R1R1.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R1R1.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R1R1.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_02_3_EBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_02_3_EB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_EB.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_EB.DESCRIPTION",
                "enocean-shortcut-EB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_EBEB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_EBEB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_EBEB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_EBEB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_02_3_R2Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_02_3_R2_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R2.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R2.DESCRIPTION",
                "enocean-shortcut-R2", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R2R2.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R2R2.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "4", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R2R2.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_R2R2.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_02_3_SATemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_02_3_SA_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_SA.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_SA.DESCRIPTION",
                "enocean-shortcut-SA", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_SASA.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_SASA.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "7", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_SASA.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_3.SHORTCUT_SASA.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddTypesF6_02_02_4Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_02_02_4Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_02_02_4Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.DESCRIPTION",
                "enocean-type-02", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_02_02_4_R1Templates(factory, interfaceGuid);
AddDataFieldF6_02_02_4_EBTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_02_02_4_R1Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_02_4_R1_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_R1.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_R1.DESCRIPTION",
                "enocean-shortcut-R1", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_R1R1.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_R1R1.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_R1R1.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_R1R1.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_02_4_EBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_02_4_EB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_EB.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_EB.DESCRIPTION",
                "enocean-shortcut-EB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_EBEB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_EBEB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_EBEB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_02_4.SHORTCUT_EBEB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddTypesF6_02_03_5Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_02_03_5Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_02_03_5Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.DESCRIPTION",
                "enocean-type-02", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_02_03_5_RATemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_02_03_5_RATemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_03_5_RA_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.SHORTCUT_RA.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.SHORTCUT_RA.DESCRIPTION",
                "enocean-shortcut-RA", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.SHORTCUT_RARA.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.SHORTCUT_RARA.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.SHORTCUT_RARA.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_03_5.SHORTCUT_RARA.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "8", 0, 0);

                       
        }
        private static void AddTypesF6_02_04_6Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_02_04_6Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_02_04_6Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.DESCRIPTION",
                "enocean-type-02", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_02_04_6_EBOTemplates(factory, interfaceGuid);
AddDataFieldF6_02_04_6_BCTemplates(factory, interfaceGuid);
AddDataFieldF6_02_04_6_RBITemplates(factory, interfaceGuid);
AddDataFieldF6_02_04_6_RB0Templates(factory, interfaceGuid);
AddDataFieldF6_02_04_6_RAITemplates(factory, interfaceGuid);
AddDataFieldF6_02_04_6_RA0Templates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_02_04_6_EBOTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_04_6_EBO_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_EBO.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_EBO.DESCRIPTION",
                "enocean-shortcut-EBO", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_EBOEBO.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_EBOEBO.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_EBOEBO.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_EBOEBO.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_04_6_BCTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_04_6_BC_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_BC.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_BC.DESCRIPTION",
                "enocean-shortcut-BC", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_BCBC.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_BCBC.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_BCBC.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_BCBC.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_04_6_RBITemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_04_6_RBI_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RBI.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RBI.DESCRIPTION",
                "enocean-shortcut-RBI", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RBIRBI.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RBIRBI.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "4", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RBIRBI.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RBIRBI.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_04_6_RB0Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_04_6_RB0_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RB0.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RB0.DESCRIPTION",
                "enocean-shortcut-RB0", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RB0RB0.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RB0RB0.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "5", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RB0RB0.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RB0RB0.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_04_6_RAITemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_04_6_RAI_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RAI.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RAI.DESCRIPTION",
                "enocean-shortcut-RAI", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RAIRAI.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RAIRAI.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "6", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RAIRAI.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RAIRAI.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_02_04_6_RA0Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_02_04_6_RA0_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RA0.NAME", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RA0.DESCRIPTION",
                "enocean-shortcut-RA0", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RA0RA0.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RA0RA0.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "7", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RA0RA0.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_02.TYPE_04_6.SHORTCUT_RA0RA0.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddFunctionF6_03Templates(INodeTemplateFactory factory, Guid rorgInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(FunctionF6_03Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_03.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            factory.CreateNodeTemplate(FunctionF6_03Guid, "ENOCEAN.RORG_F6.FUNCTION_03.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.DESCRIPTION",
                "enocean-function-03", rorgInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xF6, FunctionF6_03Guid);
            AddTypesF6_03_01_1Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_03_01_2Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_03_02_3Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_03_02_4Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddTypesF6_03_01_1Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_03_01_1Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_03_01_1Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.DESCRIPTION",
                "enocean-type-03", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_03_01_1_R1Templates(factory, interfaceGuid);
AddDataFieldF6_03_01_1_EBTemplates(factory, interfaceGuid);
AddDataFieldF6_03_01_1_R2Templates(factory, interfaceGuid);
AddDataFieldF6_03_01_1_SATemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_03_01_1_R1Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_01_1_R1_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R1.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R1.DESCRIPTION",
                "enocean-shortcut-R1", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R1R1.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R1R1.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R1R1.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R1R1.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_03_01_1_EBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_01_1_EB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_EB.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_EB.DESCRIPTION",
                "enocean-shortcut-EB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_EBEB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_EBEB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_EBEB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_EBEB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_03_01_1_R2Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_01_1_R2_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R2.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R2.DESCRIPTION",
                "enocean-shortcut-R2", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R2R2.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R2R2.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "4", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R2R2.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_R2R2.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_03_01_1_SATemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_01_1_SA_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_SA.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_SA.DESCRIPTION",
                "enocean-shortcut-SA", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_SASA.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_SASA.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "7", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_SASA.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_1.SHORTCUT_SASA.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddTypesF6_03_01_2Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_03_01_2Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_03_01_2Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.DESCRIPTION",
                "enocean-type-03", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_03_01_2_R1Templates(factory, interfaceGuid);
AddDataFieldF6_03_01_2_EBTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_03_01_2_R1Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_01_2_R1_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_R1.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_R1.DESCRIPTION",
                "enocean-shortcut-R1", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_R1R1.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_R1R1.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_R1R1.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_R1R1.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_03_01_2_EBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_01_2_EB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_EB.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_EB.DESCRIPTION",
                "enocean-shortcut-EB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_EBEB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_EBEB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_EBEB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_01_2.SHORTCUT_EBEB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddTypesF6_03_02_3Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_03_02_3Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_03_02_3Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.DESCRIPTION",
                "enocean-type-03", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_03_02_3_R1Templates(factory, interfaceGuid);
AddDataFieldF6_03_02_3_EBTemplates(factory, interfaceGuid);
AddDataFieldF6_03_02_3_R2Templates(factory, interfaceGuid);
AddDataFieldF6_03_02_3_SATemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_03_02_3_R1Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_02_3_R1_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R1.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R1.DESCRIPTION",
                "enocean-shortcut-R1", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R1R1.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R1R1.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R1R1.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R1R1.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_03_02_3_EBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_02_3_EB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_EB.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_EB.DESCRIPTION",
                "enocean-shortcut-EB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_EBEB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_EBEB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_EBEB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_EBEB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_03_02_3_R2Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_02_3_R2_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R2.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R2.DESCRIPTION",
                "enocean-shortcut-R2", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R2R2.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R2R2.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "4", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R2R2.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_R2R2.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_03_02_3_SATemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_02_3_SA_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_SA.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_SA.DESCRIPTION",
                "enocean-shortcut-SA", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_SASA.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_SASA.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "7", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_SASA.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_3.SHORTCUT_SASA.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddTypesF6_03_02_4Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_03_02_4Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_03_02_4Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.DESCRIPTION",
                "enocean-type-03", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_03_02_4_R1Templates(factory, interfaceGuid);
AddDataFieldF6_03_02_4_EBTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_03_02_4_R1Templates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_02_4_R1_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_R1.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_R1.DESCRIPTION",
                "enocean-shortcut-R1", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_R1R1.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_R1R1.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_R1R1.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_R1R1.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

                       
        }
        private static void AddDataFieldF6_03_02_4_EBTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_03_02_4_EB_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_EB.NAME", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_EB.DESCRIPTION",
                "enocean-shortcut-EB", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_EBEB.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_EBEB.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "3", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_EBEB.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_03.TYPE_02_4.SHORTCUT_EBEB.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddFunctionF6_04Templates(INodeTemplateFactory factory, Guid rorgInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(FunctionF6_04Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_04.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            factory.CreateNodeTemplate(FunctionF6_04Guid, "ENOCEAN.RORG_F6.FUNCTION_04.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.DESCRIPTION",
                "enocean-function-04", rorgInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xF6, FunctionF6_04Guid);
            AddTypesF6_04_01_1Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_04_01_2Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_04_02_3Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddTypesF6_04_01_1Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_04_01_1Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_04_01_1Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.DESCRIPTION",
                "enocean-type-04", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_04_01_1_KCTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_04_01_1_KCTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_04_01_1_KC_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.SHORTCUT_KC.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.SHORTCUT_KC.DESCRIPTION",
                "enocean-shortcut-KC", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.SHORTCUT_KCKC.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.SHORTCUT_KCKC.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.SHORTCUT_KCKC.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_1.SHORTCUT_KCKC.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "8", 0, 0);

                       
        }
        private static void AddTypesF6_04_01_2Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_04_01_2Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_04_01_2Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.DESCRIPTION",
                "enocean-type-04", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_04_01_2_KCTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_04_01_2_KCTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_04_01_2_KC_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.SHORTCUT_KC.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.SHORTCUT_KC.DESCRIPTION",
                "enocean-shortcut-KC", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.SHORTCUT_KCKC.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.SHORTCUT_KCKC.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.SHORTCUT_KCKC.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_01_2.SHORTCUT_KCKC.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "8", 0, 0);

                       
        }
        private static void AddTypesF6_04_02_3Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_04_02_3Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_04_02_3Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.DESCRIPTION",
                "enocean-type-04", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_04_02_3_EBOTemplates(factory, interfaceGuid);
AddDataFieldF6_04_02_3_BCTemplates(factory, interfaceGuid);
AddDataFieldF6_04_02_3_SOCTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_04_02_3_EBOTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_04_02_3_EBO_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_EBO.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_EBO.DESCRIPTION",
                "enocean-shortcut-EBO", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_EBOEBO.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_EBOEBO.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_EBOEBO.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_EBOEBO.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_04_02_3_BCTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_04_02_3_BC_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_BC.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_BC.DESCRIPTION",
                "enocean-shortcut-BC", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_BCBC.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_BCBC.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_BCBC.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_BCBC.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_04_02_3_SOCTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_04_02_3_SOC_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_SOC.NAME", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_SOC.DESCRIPTION",
                "enocean-shortcut-SOC", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_SOCSOC.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_SOCSOC.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "5", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_SOCSOC.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_04.TYPE_02_3.SHORTCUT_SOCSOC.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddFunctionF6_05Templates(INodeTemplateFactory factory, Guid rorgInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(FunctionF6_05Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_05.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            factory.CreateNodeTemplate(FunctionF6_05Guid, "ENOCEAN.RORG_F6.FUNCTION_05.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.DESCRIPTION",
                "enocean-function-05", rorgInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xF6, FunctionF6_05Guid);
            AddTypesF6_05_00_1Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_05_01_2Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_05_02_3Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddTypesF6_05_00_1Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_05_00_1Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_05_00_1Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.DESCRIPTION",
                "enocean-type-05", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_05_00_1_WNDTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_05_00_1_WNDTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_05_00_1_WND_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.SHORTCUT_WND.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.SHORTCUT_WND.DESCRIPTION",
                "enocean-shortcut-WND", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.SHORTCUT_WNDWND.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.SHORTCUT_WNDWND.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.SHORTCUT_WNDWND.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_00_1.SHORTCUT_WNDWND.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "8", 0, 0);

                       
        }
        private static void AddTypesF6_05_01_2Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_05_01_2Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_05_01_2Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.DESCRIPTION",
                "enocean-type-05", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_05_01_2_WASTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_05_01_2_WASTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_05_01_2_WAS_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.SHORTCUT_WAS.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.SHORTCUT_WAS.DESCRIPTION",
                "enocean-shortcut-WAS", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.SHORTCUT_WASWAS.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.SHORTCUT_WASWAS.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.SHORTCUT_WASWAS.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_01_2.SHORTCUT_WASWAS.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "8", 0, 0);

                       
        }
        private static void AddTypesF6_05_02_3Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_05_02_3Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_05_02_3Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.DESCRIPTION",
                "enocean-type-05", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_05_02_3_SMOTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_05_02_3_SMOTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_05_02_3_SMO_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.SHORTCUT_SMO.NAME", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.SHORTCUT_SMO.DESCRIPTION",
                "enocean-shortcut-SMO", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.SHORTCUT_SMOSMO.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.SHORTCUT_SMOSMO.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.SHORTCUT_SMOSMO.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_05.TYPE_02_3.SHORTCUT_SMOSMO.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "8", 0, 0);

                       
        }
        private static void AddFunctionF6_10Templates(INodeTemplateFactory factory, Guid rorgInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(FunctionF6_10Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_10.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            factory.CreateNodeTemplate(FunctionF6_10Guid, "ENOCEAN.RORG_F6.FUNCTION_10.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.DESCRIPTION",
                "enocean-function-10", rorgInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            enoceanFactory.AddTemplate(0xF6, FunctionF6_10Guid);
            AddTypesF6_10_00_1Templates(factory, interfaceGuid, enoceanFactory);
AddTypesF6_10_01_2Templates(factory, interfaceGuid, enoceanFactory);

        }
        private static void AddTypesF6_10_00_1Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_10_00_1Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_10_00_1Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.DESCRIPTION",
                "enocean-type-10", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_10_00_1_WINTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_10_00_1_WINTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_10_00_1_WIN_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.SHORTCUT_WIN.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.SHORTCUT_WIN.DESCRIPTION",
                "enocean-shortcut-WIN", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.SHORTCUT_WINWIN.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.SHORTCUT_WINWIN.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "0", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.SHORTCUT_WINWIN.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_00_1.SHORTCUT_WINWIN.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "8", 0, 0);

                       
        }
        private static void AddTypesF6_10_01_2Templates(INodeTemplateFactory factory, Guid funcInterfaceGuid, EnOceanTemplateFactory enoceanFactory)
        {
            Guid interfaceGuid = GenerateNewGuid(TypeF6_10_01_2Guid, 1);
            factory.CreateInterfaceType(interfaceGuid, "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.DESCRIPTION", Int32.MaxValue, int.MaxValue, true);

            var nodeGuid = TypeF6_10_01_2Guid;
            
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.DESCRIPTION",
                "enocean-type-10", funcInterfaceGuid, interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.SERIAL_NUMBER.NAME", "ENOCEAN.SERIAL_NUMBER.DESCRIPTION",
                "enocean-serialnumber", PropertyTemplateType.Text, nodeGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 0);

            enoceanFactory.AddTemplate(0xF6, nodeGuid);
            AddDataFieldF6_10_01_2_HCTemplates(factory, interfaceGuid);
AddDataFieldF6_10_01_2_HVLTemplates(factory, interfaceGuid);


        }
        private static void AddDataFieldF6_10_01_2_HCTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_10_01_2_HC_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HC.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HC.DESCRIPTION",
                "enocean-shortcut-HC", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HCHC.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HCHC.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HCHC.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HCHC.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "1", 0, 0);

                       
        }
        private static void AddDataFieldF6_10_01_2_HVLTemplates(INodeTemplateFactory factory, Guid typeInterfaceGuid)
        {
            var nodeGuid = DataFieldF6_10_01_2_HVL_Guid;
            factory.CreateNodeTemplate(nodeGuid, "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HVL.NAME", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HVL.DESCRIPTION",
                "enocean-shortcut-HVL", typeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 1), "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HVLHVL.OFFSET", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HVLHVL.OFFSET",
                "enocean-bitoffset", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "4", 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(nodeGuid, 2), "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HVLHVL.LENGTH", "ENOCEAN.RORG_F6.FUNCTION_10.TYPE_01_2.SHORTCUT_HVLHVL.LENGTH",
                "enocean-length", PropertyTemplateType.Integer, nodeGuid, "ENOCEAN", false, true, null, "4", 0, 0);

                       
        }
                }
        }