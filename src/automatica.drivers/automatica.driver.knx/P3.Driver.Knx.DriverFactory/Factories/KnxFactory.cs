using System;
using System.Runtime.CompilerServices;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using P3.Driver.Knx.DriverFactory.P3.Knx.Core.Driver;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;


[assembly: InternalsVisibleTo("P3.Driver.Knx.Tests")]

namespace P3.Driver.Knx.DriverFactory.Factories
{
    public abstract class KnxFactory : Automatica.Core.Driver.DriverFactory
    {
        public override Version DriverVersion => new Version(2, 0, 0, 2);

        // interfaces
        internal static readonly Guid KnxIpGateway3LevelInterface = new Guid("249a13fe-f287-44ff-891a-963ba8c92160");
        
        internal static readonly Guid KnxIp3LevelMainAddress = new Guid("eb439101-d54f-4719-8311-a71fc7017ab3");
        internal static readonly Guid KnxIp3LevelMiddleAddress = new Guid("1ff9bd8a-15ab-4d17-8552-a39f0668399c");
        internal static readonly Guid KnxInterface = new Guid("eb9e7115-22fa-49e0-a1e8-2293e7f07e74");

        //node types
        internal static readonly Guid KnxGatway = new Guid("5754edc3-6eb5-4d77-91c8-278e4e6c8f96");
        internal static readonly Guid KnxBaos = new Guid("569d4454-7973-4780-8b46-b3f3ae4c5733");
        internal static readonly Guid KnxSecureGatway = new Guid("ee218537-9b35-4e10-b929-418748dc1728");

        //state
        internal static readonly Guid KnxGatwayStatus = new Guid("1d3c5d3f-92c7-4c9d-9fb6-c8ba7be51408");

        //3 level knx
        internal static readonly Guid KnxInterface3Level = new Guid("a1f1b389-e5ff-4dcf-9318-a3020e8c2820");
        internal static readonly Guid KnxInterface3LevelMain = new Guid("99813fd4-bdac-4858-93ee-5ac76df1170b");
        internal static readonly Guid KnxInterface3LevelMiddle = new Guid("c0378116-2f83-4c82-ac88-7d626b29bcf0");
        internal static readonly Guid KnxInterface3LevelValue = new Guid("dae3210f-f28b-44d4-9369-1fddefe3a244");


        internal static readonly Guid UseTunnel = new Guid("dfa17c76-cc6f-42a9-94c7-19e46fa807ae");
        internal static readonly Guid OnlyUseTunnel = new Guid("9ffc84d0-947b-41de-93d8-46d26a684f71");
        internal static readonly Guid TunnelDomain = new Guid("5892b55e-7759-488d-9ce6-f182c34c3a72");

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(KnxInterface, "KNX.GATEWAY.NAME", "KNX.GATEWAY.DESCRIPTION", 2, int.MaxValue, true);
            factory.CreateInterfaceType(KnxInterface3Level, "KNX.GATEWAY_3_LEVEL.NAME", "KNX.GATEWAY_3_LEVEL.DESCRIPTION", 31, 1, false);
            factory.CreateInterfaceType(KnxInterface3LevelMain, "KNX.GATEWAY_3_LEVEL.MAIN.NAME", "KNX.GATEWAY_3_LEVEL.MAIN.DESCRIPTION", 7, 31, false);
            factory.CreateInterfaceType(KnxInterface3LevelMiddle, "KNX.GATEWAY_3_LEVEL.MIDDLE.NAME", "KNX.GATEWAY_3_LEVEL.MIDDLE.DESCRIPTION", 255, 7, false);
            factory.CreateInterfaceType(KnxInterface3LevelValue, "KNX.GATEWAY_3_LEVEL.VALUE.NAME", "KNX.GATEWAY_3_LEVEL.VALUE.DESCRIPTION", 0, 255, false);

            factory.CreateInterfaceType(KnxBaos, "KNX.BAOS.NAME", "KNX.BAOS.DESCRIPTION", 0, 1, true);

            factory.CreateNodeTemplate(KnxGatway, "KNX.GATEWAY.NAME", "KNX.GATEWAY.DESCRIPTION", "knx-gw",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), KnxInterface, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(GenerateNewGuid(KnxGatway, 1), "COMMON.PROPERTY.IP.NAME", "COMMON.PROPERTY.IP.DESCRIPTION",
                "knx-ip", PropertyTemplateType.Ip, KnxGatway, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 1,
                1);
            factory.CreatePropertyTemplate(GenerateNewGuid(KnxGatway, 2), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION",
                "knx-port", PropertyTemplateType.Range, KnxGatway, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), "3671", 1,
                2);
            factory.CreatePropertyTemplate(GenerateNewGuid(KnxGatway, 3), "COMMON.PROPERTY.USE_NAT.NAME", "COMMON.PROPERTY.USE_NAT.DESCRIPTION",
                "knx-use-nat", PropertyTemplateType.Bool, KnxGatway, "COMMON.CATEGORY.ADDRESS", true, false, "", false, 1,
                3);


            factory.CreatePropertyTemplate(UseTunnel, "COMMON.PROPERTY.ENABLE_TUNNEL.NAME", "COMMON.PROPERTY.ENABLE_TUNNEL.DESCRIPTION",
                "knx-use-tunnel", PropertyTemplateType.Bool, KnxGatway, "SERVER.REMOTE", true, false, "", false, 1,
                1); 
            
            factory.CreatePropertyTemplate(OnlyUseTunnel, "KNX.PROPERTIES.ONLY_USE_TUNNEL.NAME", "KNX.PROPERTIES.ONLY_USE_TUNNEL.DESCRIPTION",
                "knx-only-use-tunnel", PropertyTemplateType.Bool, KnxGatway, "SERVER.REMOTE", true, false, "", false, 1,
                1);

             factory.CreateNodeTemplate(KnxSecureGatway, "KNX.SECURE_GATEWAY.NAME", "KNX.SECURE_GATEWAY.DESCRIPTION", "knx-secure-gw",
                  GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), KnxInterface, false, false, true, false, true,
                  NodeDataType.NoAttribute, int.MaxValue, false);
              factory.CreatePropertyTemplate(GenerateNewGuid(KnxSecureGatway, 1), "COMMON.PROPERTY.IP.NAME", "COMMON.PROPERTY.IP.DESCRIPTION",
                  "knx-ip", PropertyTemplateType.Ip, KnxSecureGatway, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 1,
                  1);
              factory.CreatePropertyTemplate(GenerateNewGuid(KnxSecureGatway, 2), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION",
                  "knx-port", PropertyTemplateType.Range, KnxSecureGatway, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), "3671", 1,
                  2);
              factory.CreatePropertyTemplate(GenerateNewGuid(KnxSecureGatway, 3), "KNX.PROPERTIES.SECURE.AUTHPW.NAME", "KNX.PROPERTIES.SECURE.AUTHPW.DESCRIPTION",
                  "knx-auth-pw", PropertyTemplateType.Password, KnxSecureGatway, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 1,
                  3);
              factory.CreatePropertyTemplate(GenerateNewGuid(KnxSecureGatway, 4), "KNX.PROPERTIES.SECURE.USERPW.NAME", "KNX.PROPERTIES.SECURE.USERPW.DESCRIPTION",
                  "knx-user-pw", PropertyTemplateType.Password, KnxSecureGatway, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 1,
                  4);
              factory.CreatePropertyTemplate(GenerateNewGuid(KnxSecureGatway, 5), "KNX.PROPERTIES.SECURE.IA_ADDRESS.NAME", "KNX.PROPERTIES.SECURE.IA_ADDRESS.DESCRIPTION",
                  "knx-ia-address", PropertyTemplateType.Text, KnxSecureGatway, "COMMON.CATEGORY.ADDRESS", true, false, "", 0, 1,
                  5);
            factory.CreatePropertyTemplate(GenerateNewGuid(KnxSecureGatway, 6), "KNX.PROPERTIES.SECURE.USER_ID.NAME", "KNX.PROPERTIES.SECURE.USER_ID.DESCRIPTION",
                  "knx-user-id", PropertyTemplateType.Integer, KnxSecureGatway, "COMMON.CATEGORY.ADDRESS", true, false, "", 0, 1,
                  6);
            factory.CreatePropertyTemplate(GenerateNewGuid(KnxSecureGatway, 7), "COMMON.PROPERTY.USE_NAT.NAME",
                  "COMMON.PROPERTY.USE_NAT.DESCRIPTION",
                  "knx-use-nat", PropertyTemplateType.Bool, KnxSecureGatway, "COMMON.CATEGORY.ADDRESS", true, false,
                  "", false, 1,
                  7);

            factory.CreatePropertyTemplate(GenerateNewGuid(UseTunnel, 1), "COMMON.PROPERTY.ENABLE_TUNNEL.NAME", "COMMON.PROPERTY.ENABLE_TUNNEL.DESCRIPTION",
                "knx-use-tunnel", PropertyTemplateType.Bool, KnxSecureGatway, "SERVER.REMOTE", true, false, "", false, 1,
                1);
            factory.CreatePropertyTemplate(GenerateNewGuid(OnlyUseTunnel, 1), "KNX.PROPERTIES.ONLY_USE_TUNNEL.NAME", "KNX.PROPERTIES.ONLY_USE_TUNNEL.DESCRIPTION",
                "knx-only-use-tunnel", PropertyTemplateType.Bool, KnxSecureGatway, "SERVER.REMOTE", true, false, "", false, 1,
                1);


            factory.CreateNodeTemplate(KnxBaos, "KNX.BAOS.NAME", "KNX.BAOS.DESCRIPTION",
                "knx-baos", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual), KnxBaos, false, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(new Guid("86db8e85-0770-47a0-9fa9-71558113a0db"), "KNX.BAOS.DATAPOINTS.NAME", "KNX.BAOS.DATAPOINTS.DESCRIPTION",
                "knx-baos", KnxBaos, KnxInterface3LevelMiddle, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(KnxGatwayStatus, "KNX.GW_STATUS.NAME", "KNX.GW_STATUS.DESCRITPION",
                "knx-gw-state", KnxInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true,
                NodeDataType.Boolean, 1, false, false);

            factory.CreateNodeTemplate(KnxIpGateway3LevelInterface, "KNX.GATEWAY_3_LEVEL.NAME", "KNX.GATEWAY_3_LEVEL.DESCRIPTION", "knx-3-level",
                KnxInterface, KnxInterface3Level, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(KnxIpGateway3LevelInterface, 1), "COMMON.PROPERTY.IMPORT.NAME", "COMMON.PROPERTY.IMPORT.DESCRIPTION",
                "knx-import", PropertyTemplateType.ImportData, KnxIpGateway3LevelInterface, "COMMON.CATEGORY.IMPORT", true, false, "OBJECT_SAVED", false, 2,
                1);

            factory.CreateNodeTemplate(KnxIp3LevelMainAddress, "KNX.GATEWAY_3_LEVEL.MAIN.NAME", "KNX.GATEWAY_3_LEVEL.MAIN.DESCRIPTION", "knx-3-level-main",
                KnxInterface3Level, KnxInterface3LevelMain, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);
            AddAddressProperty(KnxIp3LevelMainAddress, factory, 31);
            factory.ChangeNodeTemplateMetaName(KnxIp3LevelMainAddress, "{PROPERTY:knx-address:2}{CONST:-}{NODE:Name}");

            factory.CreateNodeTemplate(KnxIp3LevelMiddleAddress, "KNX.GATEWAY_3_LEVEL.MIDDLE.NAME", "KNX.GATEWAY_3_LEVEL.MIDDLE.DESCRIPTION", "knx-3-level-middle",
                KnxInterface3LevelMain, KnxInterface3LevelMiddle, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);
            AddAddressProperty(KnxIp3LevelMiddleAddress, factory, 7);
            factory.ChangeNodeTemplateMetaName(KnxIp3LevelMiddleAddress, "{PROPERTY:knx-address:2}{CONST:-}{NODE:Name}");

            AddDpt1Nodes(factory, KnxInterface3LevelMiddle);
            AddDpt2Nodes(factory, KnxInterface3LevelMiddle);
            AddDpt3Nodes(factory, KnxInterface3LevelMiddle);

            AddDpt5Nodes(factory, KnxInterface3LevelMiddle);
            AddDpt6Nodes(factory, KnxInterface3LevelMiddle);
            AddDpt7Nodes(factory, KnxInterface3LevelMiddle);
            AddDpt8Nodes(factory, KnxInterface3LevelMiddle);
            AddDpt9Nodes(factory, KnxInterface3LevelMiddle);

            AddDpt10Nodes(factory, KnxInterface3LevelMiddle);
            AddDpt11Nodes(factory, KnxInterface3LevelMiddle);
            AddDpt13Nodes(factory, KnxInterface3LevelMiddle);


            AddDpt16Nodes(factory, KnxInterface3LevelMiddle);
        }


        private void AddDpt1Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt1Guid = new Guid("e2fcfcb9-b08b-4597-9ce1-6d604fdbb510");
            factory.CreateNodeTemplate(dpt1Guid, "KNX.DPT1.NAME", "KNX.DPT1.DESCRIPTION", "knx-dpt1",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Boolean, int.MaxValue, false);

            factory.ChangeDefaultVisuTemplate(dpt1Guid, VisuMobileObjectTemplateTypes.ToggleButton);
            
            InitDptType((int)DptType.Dpt1, dpt1Guid, factory);
        }


        private void AddDpt2Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt2InterfaceGuid = new Guid("3772b020-0f19-4fd2-81a2-a426ffe55762");
            factory.CreateInterfaceType(dpt2InterfaceGuid, "KNX.DPT2.NAME", "KNX.DPT2.DESCRIPTION", 2, Int32.MaxValue,
                false);

            var dpt2Guid = new Guid("56fb9bdb-2b65-4b25-a4eb-d70e46aff966");
            factory.CreateNodeTemplate(dpt2Guid, "KNX.DPT2.NAME", "KNX.DPT2.DESCRIPTION", "knx-dpt2",
                parentNode, dpt2InterfaceGuid, false, true, false, true, false,
                NodeDataType.NoAttribute, int.MaxValue, false);
            InitDptType((int)DptType.Dpt2, dpt2Guid, factory);

            var dpt2Control = new Guid("c80e2f6a-8fe2-4e46-bd0f-4a40641f80d9");
            var dpt2Value = new Guid("bff98aa9-2e28-4b1e-91cc-5e1203627bc2");

            factory.CreateNodeTemplate(dpt2Control, "KNX.DPT2.CONTROL.NAME", "KNX.DPT2.CONTROL.DESCRIPTION",
                "knx-dpt2-control", dpt2InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(dpt2Value, "KNX.DPT2.VALUE.NAME", "KNX.DPT2.VALUE.DESCRIPTION",
                "knx-dpt2-value", dpt2InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);
        }

        private void AddDpt3Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt3InterfaceGuid = new Guid("45cdff99-5710-499c-b304-12141d4b6913");

            factory.CreateInterfaceType(dpt3InterfaceGuid, "KNX.DPT3.NAME", "KNX.DPT3.DESCRIPTION", 2, Int32.MaxValue,
                false);

            var dpt3Guid = new Guid("99d97125-b566-4cac-8210-3ba792db3c04");
            factory.CreateNodeTemplate(dpt3Guid, "KNX.DPT3.NAME", "KNX.DPT3.DESCRIPTION", "knx-dpt3",
                parentNode, dpt3InterfaceGuid, false, true, false, true, false,
                NodeDataType.NoAttribute, int.MaxValue, false);
            InitDptType((int)DptType.Dpt2, dpt3Guid, factory);


            var dpt3Control = new Guid("40610606-5306-497c-8ebc-f064cc70b005");
            var dpt3StepCode = new Guid("c3ba5a94-83ba-44d6-b853-ef0c945318c0");

            factory.CreateNodeTemplate(dpt3Control, "KNX.DPT3.CONTROL.NAME", "KNX.DPT3.CONTROL.DESCRIPTION",
                "knx-dpt2-control", dpt3InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(dpt3StepCode, "KNX.DPT3.STEPCODE.NAME", "KNX.DPT3.STEPCODE.DESCRIPTION",
                "knx-dpt2-step-code", dpt3InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);
        }

        private void AddDpt5Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt5Guid = new Guid("675c7e19-8b53-4c29-8391-ef855c2f9828");
            factory.CreateNodeTemplate(dpt5Guid, "KNX.DPT5.NAME", "KNX.DPT5.DESCRIPTION", "knx-dpt5",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Double, int.MaxValue, false);

            AddAddressProperty(dpt5Guid, factory);
            factory.CreatePropertyTemplate(GenerateNewGuid(dpt5Guid, 2), "KNX.PROPERTIES.DPT.NAME",
                "KNX.PROPERTIES.DPT.DESCRIPTION", "knx-dpt", PropertyTemplateType.Enum, dpt5Guid,
                "KNX.GROUP.DPT", true, false, PropertyHelper.CreateEnumMetaString(typeof(Dpt5Type)), (int)Dpt5Type.Dpt5_001, 0, 0);
        }

        private void AddDpt6Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt6Guid = new Guid("f1604827-b835-488c-b446-4c8e162a6b02");
            factory.CreateNodeTemplate(dpt6Guid, "KNX.DPT6.NAME", "KNX.DPT6.DESCRIPTION", "knx-dpt6",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, int.MaxValue, false);
            AddAddressProperty(dpt6Guid, factory);
            factory.CreatePropertyTemplate(GenerateNewGuid(dpt6Guid, 2), "KNX.PROPERTIES.DPT.NAME",
                "KNX.PROPERTIES.DPT.DESCRIPTION", "knx-dpt", PropertyTemplateType.Enum, dpt6Guid,
                "KNX.GROUP.DPT", true, false, PropertyHelper.CreateEnumMetaString(typeof(Dpt6Type)), (int)Dpt6Type.Dpt6_001, 0, 0);

            
            var dpt620InterfaceGuid = new Guid("cdce207b-5371-413b-bcb5-3512bb51205d");
            factory.CreateInterfaceType(dpt620InterfaceGuid, "KNX.DPT6.020.NAME", "KNX.DPT6.020.DESCRIPTION", 5,
                int.MaxValue, false);

            var dpt620Guid = new Guid("3855b29e-3fee-49c7-9412-1a7e31a853cc");
            factory.CreateNodeTemplate(dpt620Guid, "KNX.DPT6.020.NAME", "KNX.DPT6.020.DESCRIPTION", "knx-dpt6.020",
                parentNode, dpt620InterfaceGuid, false, true, false, true, false,
                NodeDataType.NoAttribute, int.MaxValue, false);
            InitDptType((int)DptType.Dpt6_020, dpt620Guid, factory);

            var dpt620AGuid = new Guid("543b5ab6-4540-4338-a104-01a81962cce4");
            factory.CreateNodeTemplate(dpt620AGuid, "KNX.DPT6.020.A.NAME", "KNX.DPT6.020.A.DESCRIPTION", "knx-dpt6.020.A",
                dpt620InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            var dpt620BGuid = new Guid("5969d9e4-4ae7-4dce-946b-46782c5cc768");
            factory.CreateNodeTemplate(dpt620BGuid, "KNX.DPT6.020.B.NAME", "KNX.DPT6.020.B.DESCRIPTION", "knx-dpt6.020.B",
                dpt620InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            var dpt620CGuid = new Guid("9251fd6b-aa09-41f2-a225-119f6c367fd5");
            factory.CreateNodeTemplate(dpt620CGuid, "KNX.DPT6.020.C.NAME", "KNX.DPT6.020.C.DESCRIPTION", "knx-dpt6.020.C",
                dpt620InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            var dpt620DGuid = new Guid("2ef86e06-e40b-4b56-b1ec-3a194b6ce763");
            factory.CreateNodeTemplate(dpt620DGuid, "KNX.DPT6.020.D.NAME", "KNX.DPT6.020.D.DESCRIPTION", "knx-dpt6.020.D",
                dpt620InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            var dpt620EGuid = new Guid("b08ed54a-ade3-471b-b91b-3b747d712ec3");
            factory.CreateNodeTemplate(dpt620EGuid, "KNX.DPT6.020.E.NAME", "KNX.DPT6.020.E.DESCRIPTION", "knx-dpt6.020.E",
                dpt620InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Boolean, 1, false);

            var dpt620ActiveModeGuid = new Guid("8eef4beb-dfc1-46e1-99e6-3b6210b4347a");
            factory.CreateNodeTemplate(dpt620ActiveModeGuid, "KNX.DPT6.020.ACTIVE_MODE.NAME", "KNX.DPT6.020.ACTIVE_MODE.DESCRIPTION", "knx-dpt6.020.ACTIVE_MODE",
                dpt620InterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, false, true, false,
                NodeDataType.Integer, 1, false);
        }

        private void AddDpt7Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt7Guid = new Guid("d5ef64da-4ef2-44b2-806f-5cc5bd06228c");
            factory.CreateNodeTemplate(dpt7Guid, "KNX.DPT7.NAME", "KNX.DPT7.DESCRIPTION", "knx-dpt7",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, int.MaxValue, false);
            AddAddressProperty(dpt7Guid, factory);

            factory.CreatePropertyTemplate(GenerateNewGuid(dpt7Guid, 2), "KNX.PROPERTIES.DPT.NAME",
                "KNX.PROPERTIES.DPT.DESCRIPTION", "knx-dpt", PropertyTemplateType.Enum, dpt7Guid,
                "KNX.GROUP.DPT", true, false, PropertyHelper.CreateEnumMetaString(typeof(Dpt7Type)), (int)Dpt7Type.Dpt7_001, 0, 0);
        }

        private void AddDpt8Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt8Guid = new Guid("f9c2c787-d9bb-4207-b8b3-5e786e7757a2");
            factory.CreateNodeTemplate(dpt8Guid, "KNX.DPT8.NAME", "KNX.DPT8.DESCRIPTION", "knx-dpt8",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Integer, int.MaxValue, false);
            AddAddressProperty(dpt8Guid, factory);

            factory.CreatePropertyTemplate(GenerateNewGuid(dpt8Guid, 2), "KNX.PROPERTIES.DPT.NAME",
                "KNX.PROPERTIES.DPT.DESCRIPTION", "knx-dpt", PropertyTemplateType.Enum, dpt8Guid,
                "KNX.GROUP.DPT", true, false, PropertyHelper.CreateEnumMetaString(typeof(Dpt8Type)), (int)Dpt8Type.Dpt8_002, 0, 0);
        }

        private void AddDpt9Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt9Guid = new Guid("cdd88e9f-7dbe-48cc-8c75-0bc91c7e6751");
            factory.CreateNodeTemplate(dpt9Guid, "KNX.DPT9.NAME", "KNX.DPT9.DESCRIPTION", "knx-dpt9",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Double, int.MaxValue, false);
            AddAddressProperty(dpt9Guid, factory);


            factory.CreatePropertyTemplate(GenerateNewGuid(dpt9Guid, 2), "KNX.PROPERTIES.DPT.NAME",
                "KNX.PROPERTIES.DPT.DESCRIPTION", "knx-dpt", PropertyTemplateType.Enum, dpt9Guid,
                "KNX.GROUP.DPT", true, false, PropertyHelper.CreateEnumMetaString(typeof(Dpt9Type)), (int)Dpt9Type.Dpt9_001, 0, 0);

        }


        private void AddDpt10Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt10Guid = new Guid("d17b532e-a63a-431e-b125-a305ac3783d4");
            factory.CreateNodeTemplate(dpt10Guid, "KNX.DPT10.NAME", "KNX.DPT10.DESCRIPTION", "knx-dpt10",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Time, int.MaxValue, false);

            InitDptType((int)DptType.Dpt10, dpt10Guid, factory);
        }
        private void AddDpt11Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt11Guid = new Guid("cae0b84b-b5b7-4399-b8e1-26ad849dd3ac");
            factory.CreateNodeTemplate(dpt11Guid, "KNX.DPT11.NAME", "KNX.DPT11.DESCRIPTION", "knx-dpt11",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Date, int.MaxValue, false);

            InitDptType((int)DptType.Dpt11, dpt11Guid, factory);
        }

        private void AddDpt13Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt13Guid = new Guid("94d9744e-4677-4d60-b7bc-0638cc355675");
            factory.CreateNodeTemplate(dpt13Guid, "KNX.DPT13.NAME", "KNX.DPT13.DESCRIPTION", "knx-dpt13",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.Date, int.MaxValue, false);

            InitDptType((int)DptType.Dpt13, dpt13Guid, factory);
        }

        private void AddDpt16Nodes(INodeTemplateFactory factory, Guid parentNode)
        {
            var dpt16Guid = new Guid("bd222553-42f9-4cdf-afd6-f5a5fda6a53b");
            factory.CreateNodeTemplate(dpt16Guid, "KNX.DPT16.NAME", "KNX.DPT16.DESCRIPTION", "knx-dpt16",
                parentNode, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false,
                NodeDataType.String, int.MaxValue, false);
            
            InitDptType((int)DptType.Dpt16, dpt16Guid, factory);

            factory.CreatePropertyTemplate(GenerateNewGuid(dpt16Guid, 3), "KNX.PROPERTIES.DPT-SUB.NAME",
                "KNX.PROPERTIES.DPT-SUB.DESCRIPTION", "knx-dpt-sub", PropertyTemplateType.Enum, dpt16Guid,
                "KNX.GROUP.DPT", true, false, PropertyHelper.CreateEnumMetaString(typeof(Dpt16Type)), (int)Dpt16Type.Dpt16_000, 0, 0);
        }


        private void AddAddressProperty(Guid node, INodeTemplateFactory factory)
        {
            AddAddressProperty(node, factory, 255);

            factory.ChangeNodeTemplateMetaName(node, "{PROPERTY:knx-address:3}{CONST:-}{NODE:Name}");
        }

        private void InitDptType(int type, Guid node, INodeTemplateFactory factory)
        {
            AddAddressProperty(node, factory);

            factory.CreatePropertyTemplate(GenerateNewGuid(node, 2), "KNX.PROPERTIES.DPT.NAME",
                "KNX.PROPERTIES.DPT.DESCRIPTION", "knx-dpt", PropertyTemplateType.Integer, node,
                "KNX.GROUP.DPT", false, true, "", type, 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(node, 5), "KNX.PROPERTIES.IS_READABLE_FROM_BUS.NAME", "KNX.PROPERTIES.IS_READABLE_FROM_BUS.DESCRIPTION","readable_from_bus", PropertyTemplateType.Bool,
                node, "COMMON.CATEGORY.MISC", true, false, "", 0, 0, 0);
        }
        private void AddAddressProperty(Guid nodeTemplate, INodeTemplateFactory factory, int maxAddress)
        {
            var newGuid = GenerateNewGuid(nodeTemplate, 1);
            factory.CreatePropertyTemplate(newGuid, "KNX.PROPERTIES.ADDRESS.NAME",
                "KNX.PROPERTIES.ADDRESS.DESCRIPTION", "knx-address", PropertyTemplateType.Range, nodeTemplate,
                "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(0, maxAddress), 0, 0, 0);

            factory.CreatePropertyConstraint(GenerateNewGuid(nodeTemplate, 3), "KNX.CONSTRAINT.UNIQUE_ADDRESS.NAME",
                "KNX.CONSTRAINT.UNIQUE_ADDRESS.NAME", PropertyConstraint.Unique, PropertyConstraintLevel.Warn, newGuid);
        }

        private Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[^1] = (byte)(Convert.ToInt32(gu[^1]) + c);

            return new Guid(gu);
        }
    }
}
