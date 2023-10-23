using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Migrations;
using Automatica.Core.EF.Models;
using Automatica.Driver.ShellyFactory.Discovery;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace Automatica.Driver.ShellyFactory
{
    public class ShellyFactory : DriverFactory
    {
        public static Guid DriverId = new Guid("c3cfe3fc-4440-4f8a-939a-5bdaab7f5c07");
        public static Guid Shelly1Device = new Guid("d5f3a2d0-275e-4cd3-86f8-cd358162fdff");
        public static Guid Shelly1PmDevice = new Guid("99bc456d-3bcc-437a-b6dd-a8ce38d1b901");
        public static Guid Shelly25Device = new Guid("46e1dbd5-45a2-4ed1-a6d0-d967f65579b6");


        public static Guid ShellyPlus1PmDevice = new Guid("10f3e8f5-0d46-4aa7-aa93-c66abc67775b");
        public override string DriverName => "Shelly";

        public override Guid DriverGuid => DriverId;

        public override string ImageName => "automaticacore/plugin-automatica.driver.shelly";

        public override Version DriverVersion => new Version(0, 0, 0, 5);

        public override bool InDevelopmentMode => true;


        public const string DeviceIdPropertyKey = "shelly-id";

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new ShellyDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "SHELLY.NAME", "SHELLY.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "SHELLY.NAME", "SHELLY.DESCRIPTION", "SHELLY", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("fb92c117-9a38-46f8-8f14-ade2d2b1c26a"), "SHELLY.SCAN.NAME", "SHELLY.SCAN.DESCRIPTION",
                "scan", PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0,
                0);

            InitShelly1Templates(factory);
            InitShelly1PmTemplates(factory);
            InitShelly25Templates(factory);

            InitShellyPlus1PmTemplates(factory);
        }

        private void InitShelly1Templates(INodeTemplateFactory factory)
        {
            var guid = Shelly1Device;
            factory.CreateInterfaceType(guid, "SHELLY.1.NAME", "SHELLY.1.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(guid, "SHELLY.1.NAME", "SHELLY.1.DESCRIPTION", "shelly-1", DriverGuid,
                guid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            var shelly1RelayInterface =  new Guid("c835cdf2-727e-40d3-88a2-a4180219e5b2"); 
            factory.CreateInterfaceType(shelly1RelayInterface, "SHELLY.RELAYS.NAME",
                "SHELLY.RELAYS.DESCRIPTION", 1,1, false);
            factory.CreateNodeTemplate(shelly1RelayInterface, "SHELLY.RELAYS.NAME",
                "SHELLY.RELAYS.DESCRIPTION", "shelly-relays", guid, shelly1RelayInterface,
                true, false, true, false, true, NodeDataType.NoAttribute, 1, false, true);

            var shelly1State = new Guid("05d9c7b0-dc49-4670-908a-09e792b0101a");
            CreateRelayInterface(factory, shelly1State, shelly1RelayInterface, 0, 1);

            factory.CreateNodeTemplate(shelly1State, "SHELLY.1.STATE.NAME", "SHELLY.1.STATE.DESCRIPTION", "state", shelly1RelayInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, true, false, NodeDataType.Boolean, 1, false);
            factory.CreatePropertyTemplate(GenerateNewGuid(shelly1State, 1), "SHELLY.1.RELAY_CHANNEL",
                "SHELLY.1.RELAY_CHANNEL", "shelly-relay-channel", PropertyTemplateType.Numeric, shelly1State, "COMMON.CATEGORY.ADDRESS", false, false, null, 0, 0, 0);

            InitShellyParams(factory, guid, ShellyGeneration.Gen1);
        }

        private void InitShelly1PmTemplates(INodeTemplateFactory factory)
        {
            var guid = Shelly1PmDevice;
            factory.CreateInterfaceType(guid, "SHELLY.1PM.NAME", "SHELLY.1PM.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(guid, "SHELLY.1PM.NAME", "SHELLY.1PM.DESCRIPTION", "shelly-1", DriverGuid,
                guid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            InitShellyParams(factory, guid, ShellyGeneration.Gen1);

            var shelly1RelayInterface = new Guid("f66e7c26-1341-4016-8d64-4c1b7db28357");
            factory.CreateInterfaceType(shelly1RelayInterface, "SHELLY.RELAYS.NAME",
                "SHELLY.RELAYS.DESCRIPTION", 1, 1, false);
            factory.CreateNodeTemplate(shelly1RelayInterface, "SHELLY.RELAYS.NAME",
                "SHELLY.RELAYS.DESCRIPTION", "shelly-relays", guid, shelly1RelayInterface,
                true, false, true, false, true, NodeDataType.NoAttribute, 1, false, true);

            CreateRelayInterface(factory, new Guid("e2684369-6b1a-450b-b337-ff5d0bcb1888"), shelly1RelayInterface, 0, 1);

            var shelly1PmMeterInterface = new Guid("3b21aa86-72b4-440e-bb1d-3fb15dbafeb3");
            factory.CreateInterfaceType(shelly1PmMeterInterface, "SHELLY.METERS.NAME",
                "SHELLY.METERS.DESCRIPTION", 2, 1, false);
            factory.CreateNodeTemplate(shelly1PmMeterInterface, "SHELLY.METERS.NAME",
                "SHELLY.METERS.DESCRIPTION", "shelly-meters", guid, shelly1PmMeterInterface,
                false, false, true, false, true, NodeDataType.NoAttribute, 1, false, true);

            CreateMeterInterface(factory, new Guid("0717dbbb-7609-4ccd-ae93-25455c960bf8"), shelly1PmMeterInterface, 0, 1);

        }

        private void InitShellyPlus1PmTemplates(INodeTemplateFactory factory)
        {
            var guid = ShellyPlus1PmDevice;
            factory.CreateInterfaceType(guid, "SHELLY.PLUS_1PM.NAME", "SHELLY.PLUS_1PM.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(guid, "SHELLY.PLUS_1PM.NAME", "SHELLY.PLUS_1PM.DESCRIPTION", "shelly-plus-1pm", DriverGuid,
                guid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            InitShellyParams(factory, guid, ShellyGeneration.Gen2);

            var shelly1RelayInterface = new Guid("1483043e-b0de-4598-8af4-1cf218f95978");
            factory.CreateInterfaceType(shelly1RelayInterface, "SHELLY.RELAYS.NAME",
                "SHELLY.RELAYS.DESCRIPTION", 1, 1, false);
            factory.CreateNodeTemplate(shelly1RelayInterface, "SHELLY.RELAYS.NAME",
                "SHELLY.RELAYS.DESCRIPTION", "shelly-relays", guid, shelly1RelayInterface,
                true, false, true, false, true, NodeDataType.NoAttribute, 1, false, true);

            CreateRelayInterface(factory, new Guid("2984e4bb-62ff-499e-b4ba-5dbc3d09d210"), shelly1RelayInterface, 0, 1);

            var shelly1PmMeterInterface = new Guid("eefadb8d-d3ed-49e4-bbb0-9deb2e494324");
            factory.CreateInterfaceType(shelly1PmMeterInterface, "SHELLY.METERS.NAME",
                "SHELLY.METERS.DESCRIPTION", 2, 1, false);
            factory.CreateNodeTemplate(shelly1PmMeterInterface, "SHELLY.METERS.NAME",
                "SHELLY.METERS.DESCRIPTION", "shelly-meters", guid, shelly1PmMeterInterface,
                false, false, true, false, true, NodeDataType.NoAttribute, 1, false, true);

            CreateMeterInterface(factory, new Guid("75608f3f-156f-4213-b00a-7b9fdb4f19cc"), shelly1PmMeterInterface, 0, 1);

        }

        private void InitShelly25Templates(INodeTemplateFactory factory)
        {
            var guid = Shelly25Device;
            factory.CreateInterfaceType(guid, "SHELLY.25.NAME", "SHELLY.25.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(guid, "SHELLY.25.NAME", "SHELLY.25.DESCRIPTION", "shelly-25", DriverGuid,
                guid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            InitShellyParams(factory, guid, ShellyGeneration.Gen1);

            var shelly25RelayInterface = new Guid("17078292-3f92-40f2-945c-0025ab93b40a");
            factory.CreateInterfaceType(shelly25RelayInterface, "SHELLY.RELAYS.NAME",
                "SHELLY.RELAYS.DESCRIPTION", 2, 1, false);
            factory.CreateNodeTemplate(shelly25RelayInterface, "SHELLY.RELAYS.NAME",
                "SHELLY.RELAYS.DESCRIPTION", "shelly-relays", guid, shelly25RelayInterface,
                false, false, true, false, true, NodeDataType.NoAttribute, 1, false, true);

            CreateRelayInterface(factory, new Guid("70f8f8d3-5eb7-444c-926a-4df6c45910ec"), shelly25RelayInterface, 0, 2);
            CreateRelayInterface(factory, new Guid("6edb9ab6-1dc1-4966-ad79-49ffbd14cff8"), shelly25RelayInterface, 1, 2);

            var shelly25RollerInterface = new Guid("85ca112e-c59a-4a0f-8e97-1e71022b4b75");
            factory.CreateInterfaceType(shelly25RollerInterface, "SHELLY.ROLLER.NAME",
                "SHELLY.ROLLER.DESCRIPTION", 2, 1, false);
            factory.CreateNodeTemplate(shelly25RollerInterface, "SHELLY.ROLLER.NAME",
                "SHELLY.ROLLER.DESCRIPTION", "shelly-rollers", guid, shelly25RollerInterface,
                false, false, true, false, true, NodeDataType.NoAttribute, 1, false, true);

            CreateRollerInterface(factory, new Guid("c6d37a26-1c58-440f-84b8-12ff2118e99c"), shelly25RollerInterface, 0,1);

            var shelly25MeterInterface = new Guid("4cfc6c5c-aa55-4913-90a0-29a0847123cf");
            factory.CreateInterfaceType(shelly25MeterInterface, "SHELLY.METERS.NAME",
                "SHELLY.METERS.DESCRIPTION", 2, 1, false);
            factory.CreateNodeTemplate(shelly25MeterInterface, "SHELLY.METERS.NAME",
                "SHELLY.METERS.DESCRIPTION", "shelly-meters", guid, shelly25MeterInterface,
                false, false, true, false, true, NodeDataType.NoAttribute, 1, false, true);

            CreateMeterInterface(factory, new Guid("15933031-b0fe-4663-a5ef-9b59bd8bb0e5"), shelly25MeterInterface, 0, 2);
            CreateMeterInterface(factory, new Guid("013a4fd6-f448-42e0-954e-970dc785a178"), shelly25MeterInterface, 1, 2);

        }

        private void CreateRollerInterface(INodeTemplateFactory factory, Guid id, Guid needsInterface, int rollerIndex,
            int maxRollers)
        {
            var nextId = 1;
            var relayInterfaceId = GenerateNewGuid(id, nextId++);
            factory.CreateInterfaceType(relayInterfaceId, "SHELLY.ROLLER.NAME",
                "SHELLY.ROLLER.DESCRIPTION", 2, maxRollers, false);
            var relayId = GenerateNewGuid(id, nextId++);
            factory.CreateNodeTemplate(relayId, "SHELLY.ROLLER.NAME", "SHELLY.ROLLER.DESCRIPTIONS", "roller", needsInterface,
                relayInterfaceId, true, true, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(id, nextId++), "SHELLY.ROLLER",
                "SHELLY.ROLLER", "shelly-roller-channel", PropertyTemplateType.Numeric, relayId, "COMMON.CATEGORY.ADDRESS", false, false, null, rollerIndex, 0, 0);

            if (maxRollers > 1)
            {
                factory.ChangeNodeTemplateMetaName(relayId, "{NODE:Name}{CONST:-}{PROPERTY:shelly-roller-channel}");
            }

            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.ROLLER.POSITION.NAME", "SHELLY.ROLLER.POSITION.DESCRIPTION", "roller-position", relayInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, true, false, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.ROLLER.STATE.NAME", "SHELLY.ROLLER.STATE.DESCRIPTION", "roller-state", relayInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, true, false, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.ROLLER.STOP_REASON.NAME", "SHELLY.ROLLER.STOP_REASON.DESCRIPTION", "roller-stop-reason", relayInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, true, false, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.ROLLER.LAST_DIRECTION.NAME", "SHELLY.ROLLER.LAST_DIRECTION.DESCRIPTION", "roller-last-direction", relayInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, true, false, NodeDataType.Integer, 1, false);

        }

        private void CreateRelayInterface(INodeTemplateFactory factory, Guid id, Guid needsInterface, int relayIndex,
            int maxRelays)
        {
            var nextId = 1;
            var relayInterfaceId = GenerateNewGuid(id, nextId++);
            factory.CreateInterfaceType(relayInterfaceId, "SHELLY.RELAY.NAME",
                "SHELLY.RELAY.DESCRIPTION", 2, maxRelays, false);
            var relayId = GenerateNewGuid(id, nextId++);
            factory.CreateNodeTemplate(relayId, "SHELLY.RELAY.NAME", "SHELLY.RELAY.DESCRIPTIONS", "relay", needsInterface,
                relayInterfaceId, true, true, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(id, nextId++), "SHELLY.RELAY",
                "SHELLY.RELAY", "shelly-relay-channel", PropertyTemplateType.Numeric, relayId, "COMMON.CATEGORY.ADDRESS", false, false, null, relayIndex, 0, 0);

            if (maxRelays > 1)
            {
                factory.ChangeNodeTemplateMetaName(relayId, "{NODE:Name}{CONST:-}{PROPERTY:shelly-relay-channel}");
            }

            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.RELAY.STATUS.NAME", "SHELLY.RELAY.STATUS.DESCRIPTIONS", "relay-state", relayInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, true, false,
                NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.RELAY.HAS_TIMER.NAME", "SHELLY.RELAY.HAS_TIMER.DESCRIPTIONS", "relay-timer", relayInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true,
                NodeDataType.Boolean, 1, false);


        }

        private void CreateMeterInterface(INodeTemplateFactory factory, Guid id, Guid needsInterface, int meterIndex, int maxMeter)
        {
            var nextId = 1;
            var meterInterfaceId = GenerateNewGuid(id, nextId++);
            factory.CreateInterfaceType(meterInterfaceId, "SHELLY.METER.NAME",
                "SHELLY.METER.DESCRIPTION", 2, maxMeter, false);
            var meterId = GenerateNewGuid(id, nextId++);
            factory.CreateNodeTemplate(meterId, "SHELLY.METER.NAME", "SHELLY.METER.DESCRIPTIONS", "meter", needsInterface,
                meterInterfaceId, true, true, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(id, nextId++), "SHELLY.METER_CHANNEL",
                "SHELLY.METER_CHANNEL", "shelly-meter-channel", PropertyTemplateType.Numeric, meterId, "COMMON.CATEGORY.ADDRESS", false, false, null, meterIndex, 0, 0);

            if (maxMeter > 1)
            {
                factory.ChangeNodeTemplateMetaName(meterId, "{NODE:Name}{CONST:-}{PROPERTY:shelly-meter-channel}");
            }

            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.METER.POWER.NAME", "SHELLY.METER.POWER.DESCRIPTIONS", "meter-power", meterInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true,
                NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.METER.OVERPOWER.NAME", "SHELLY.METER.OVERPOWER.DESCRIPTIONS", "meter-overpower", meterInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true,
                NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.METER.IS_VALID.NAME", "SHELLY.METER.IS_VALID.DESCRIPTIONS", "meter-is-valid", meterInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true,
                NodeDataType.Boolean, 1, false);

            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.METER.TIMESTAMP.NAME", "SHELLY.METER.TIMESTAMP.DESCRIPTIONS", "meter-timestamp", meterInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true,
                NodeDataType.DateTime, 1, false);
            factory.CreateNodeTemplate(GenerateNewGuid(id, nextId++), "SHELLY.METER.TOTAL.NAME", "SHELLY.METER.TOTAL.DESCRIPTIONS", "meter-total", meterInterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true,
                NodeDataType.Integer, 1, false);
        }


        private void InitShellyParams(INodeTemplateFactory factory, Guid parentGuid, ShellyGeneration generation)
        {
            factory.CreateNodeTemplate(GenerateNewGuid(parentGuid, 5), "SHELLY.TEMPERATURE.NAME", "SHELLY.TEMPERATURE.DESCRIPTIONS", "temperature", parentGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(GenerateNewGuid(parentGuid, 6), "SHELLY.HAS_UPDATE.NAME", "SHELLY.HAS_UPDATE.DESCRIPTIONS", "has_update", parentGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true,
                NodeDataType.Boolean, 1, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(parentGuid, 4), "SHELLY.PROPERTIES.POLLING_INTERVAL.NAME",
                "SHELLY.PROPERTIES.POLLING_INTERVAL.DESCRIPTION", "polling-interval", PropertyTemplateType.Integer, parentGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, 2000, 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(parentGuid, 1), "SHELLY.PROPERTIES.ID.NAME",
                "SHELLY.PROPERTIES.ID.DESCRIPTION", DeviceIdPropertyKey, PropertyTemplateType.Text, parentGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, 0, 0, 1);

            if(generation == ShellyGeneration.Gen1) 
            {
                factory.CreatePropertyTemplate(GenerateNewGuid(parentGuid, 2), "SHELLY.PROPERTIES.USERNAME.NAME",
                "SHELLY.PROPERTIES.USERNAME.DESCRIPTION", "shelly-username", PropertyTemplateType.Text, parentGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 2, 3);
            }

            factory.CreatePropertyTemplate(GenerateNewGuid(parentGuid, 3), "SHELLY.PROPERTIES.PASSWORD.NAME",
                "SHELLY.PROPERTIES.PASSWORD.DESCRIPTION", "shelly-password", PropertyTemplateType.Password, parentGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 0, 4);

            factory.CreatePropertyTemplate(GenerateNewGuid(parentGuid, 6), "SHELLY.PROPERTIES.USE_IP.NAME",
                "SHELLY.PROPERTIES.USE_IP.DESCRIPTION", "shelly-use-ip", PropertyTemplateType.Bool, parentGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, false, 0, 5);
            factory.CreatePropertyTemplate(GenerateNewGuid(parentGuid, 7), "SHELLY.PROPERTIES.IP.NAME",
                "SHELLY.PROPERTIES.IP.DESCRIPTION", "shelly-ip", PropertyTemplateType.Ip, parentGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 0, 6);


            factory.CreatePropertyTemplate(GenerateNewGuid(parentGuid, 10), "SHELLY.PROPERTIES.GENERATION.NAME",
                "SHELLY.PROPERTIES.GENERATION.DESCRIPTION", "shelly-generation", PropertyTemplateType.Integer, parentGuid, "COMMON.CATEGORY.ADDRESS", false, false, null, (int)generation, 0, 0);
        }

        private Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[^1] = (byte)(Convert.ToInt32(gu[^1]) + c);

            return new Guid(gu);
        }
    }
}
