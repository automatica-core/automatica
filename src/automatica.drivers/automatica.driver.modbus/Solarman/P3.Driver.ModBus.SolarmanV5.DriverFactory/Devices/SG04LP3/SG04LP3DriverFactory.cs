using System;
using System.Globalization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices.SG04LP3
{
    public class SG04LP3DriverFactory : SolarmanDriverFactory
    {
        public Guid DeviceTypeGuid => new("515b4b7a-ec2a-4be4-88d9-b4e24ff7ab86");
        public Guid BatteryTypeGuid => new("4bc347d0-2fce-41ac-aa47-c9483d5cba15");
        public Guid GridTypeGuid => new("f392092f-b41f-4962-b619-882a91778f2b");
        public Guid SolarTypeGuid => new("c869500c-0437-4f8d-b553-a1fd0fdb4b42");
        public Guid UploadTypeGuid => new("d4ff173f-005c-42a1-93e5-9fa291c6cfcb");
        public Guid InverterTypeGuid => new("5820422f-bd06-492b-a22a-ded3eee85f48");
        public Guid AlertTypeGuid => new("297bcf84-1697-4430-b5ec-5f4f99f0a3d6");
        public Guid TimeOfUseTypeGuid => new("dc97b195-8596-4c04-8f9e-ee8943915866");

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DeviceTypeGuid, "SOLARMAN.DEVICE.NAME", "SOLARMAN.DEVICE.DESCRIPTION", 8, 1,
                true);
            // only the device is a driver interface: a group flagged as one makes the core start it as a driver of its own
            // when it is added ("No factory found") instead of reloading the Solarman driver, so new nodes stay without values
            factory.CreateInterfaceType(BatteryTypeGuid, "SOLARMAN.BATTERY.NAME", "SOLARMAN.BATTERY.DESCRIPTION", int.MaxValue, 1,
                false);
            factory.CreateInterfaceType(SolarTypeGuid, "SOLARMAN.SOLAR.NAME", "SOLARMAN.SOLAR.DESCRIPTION", int.MaxValue, 1,
                false);
            factory.CreateInterfaceType(GridTypeGuid, "SOLARMAN.GRID.NAME", "SOLARMAN.GRID.DESCRIPTION", int.MaxValue, 1,
                false);
            factory.CreateInterfaceType(UploadTypeGuid, "SOLARMAN.UPLOAD.NAME", "SOLARMAN.UPLOAD.DESCRIPTION", int.MaxValue, 1,
                false);
            factory.CreateInterfaceType(AlertTypeGuid, "SOLARMAN.ALERT.NAME", "SOLARMAN.ALERT.DESCRIPTION", 6, 1,
                false);
            factory.CreateInterfaceType(InverterTypeGuid, "SOLARMAN.INVERTER.NAME", "SOLARMAN.INVERTER.DESCRIPTION", int.MaxValue, 1,
                false);
            factory.CreateInterfaceType(TimeOfUseTypeGuid, "SOLARMAN.TIME_OF_USE.NAME", "SOLARMAN.TIME_OF_USE.DESCRIPTION", int.MaxValue, 1,
                false);

            factory.CreateNodeTemplate(DriverGuid, "SOLARMAN.DEVICE.SG03LP3.NAME", "SOLARMAN.DEVICE.SG03LP3.DESCRIPTION",
                "solarman-device", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DeviceTypeGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(new Guid("cb1de7ee-ccb7-4b81-9f65-dda94413902b"), "COMMON.PROPERTY.IP.NAME",
                "COMMON.PROPERTY.IP.DESCRIPTION", "solarman-ip", PropertyTemplateType.Ip, DriverGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);
            factory.CreatePropertyTemplate(new Guid("1f4c10d5-7187-412f-b008-d950daaf748d"), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION",
                "solarman-port", PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), 8899, 1, 1);


            factory.CreatePropertyTemplate(new Guid("8b0e5b4f-81f8-4169-a07e-8c777d144724"), "SOLARMAN.DEVICE.SERIAL.NAME",
                "SOLARMAN.DEVICE.SERIAL.DESCRIPTION", "solarman-serial", PropertyTemplateType.Text, DriverGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);
            factory.CreatePropertyTemplate(new Guid("8ee14fce-70fd-4861-9337-3a43344d406f"),
                "SOLARMAN.PROPERTY.POLL_INTERVAL.NAME", "SOLARMAN.PROPERTY.POLL_INTERVAL.DESCRIPTION", "solarman-poll-interval",
                PropertyTemplateType.Integer, DriverGuid, "COMMON.CATEGORY.DEVICE", true, false,
                PropertyHelper.CreateRangeMetaString(60000, 65535), 60000, 1, 1);

            factory.CreatePropertyTemplate(new Guid("b1dd2c51-0f51-4f00-91e8-179d00e48feb"), "SOLARMAN.DEVICE.ID", "", "solarman-device-id",
                PropertyTemplateType.Integer, DriverGuid, "SOLARMAN.CATEGORY.DEVICE", true, false, "", 1, 0, 1);


            factory.CreateNodeTemplate(BatteryTypeGuid, "SOLARMAN.BATTERY.NAME", "SOLARMAN.BATTERY.DESCRIPTION",
                "solarman-group.battery", DeviceTypeGuid, BatteryTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(SolarTypeGuid, "SOLARMAN.SOLAR.NAME", "SOLARMAN.SOLAR.DESCRIPTION",
                "solarman-group.solar", DeviceTypeGuid, SolarTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(GridTypeGuid, "SOLARMAN.GRID.NAME", "SOLARMAN.GRID.DESCRIPTION",
                "solarman-group.grid", DeviceTypeGuid, GridTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(UploadTypeGuid, "SOLARMAN.LOAD.NAME", "SOLARMAN.LOAD.DESCRIPTION",
                "solarman-group.load", DeviceTypeGuid, UploadTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(AlertTypeGuid, "SOLARMAN.ALERT.NAME", "SOLARMAN.ALERT.DESCRIPTION",
                "solarman-group.alert", DeviceTypeGuid, AlertTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(InverterTypeGuid, "SOLARMAN.INVERTER.NAME", "SOLARMAN.INVERTER.DESCRIPTION",
                "solarman-group.inverter", DeviceTypeGuid, InverterTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(TimeOfUseTypeGuid, "SOLARMAN.TIME_OF_USE.NAME", "SOLARMAN.TIME_OF_USE.DESCRIPTION",
                "solarman-group.time-of-use", DeviceTypeGuid, TimeOfUseTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(new Guid("a548fcc3-e0e0-43cb-80c0-390bf23317b7"), "SOLARMAN.LAST_POLL_TIMESTAMP.NAME", "SOLARMAN.LAST_POLL_TIMESTAMP.DESCRIPTION",
                "last-poll-timestamp", DeviceTypeGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true,
                NodeDataType.DateTime, 1, false);

            CreateValueTemplate(factory, SolarTypeGuid, new Guid("9b981673-466f-4b89-be49-4d3dfe6db799"), "pv1-power", NodeDataType.Double);
            CreateValueTemplate(factory, SolarTypeGuid, new Guid("992e1e54-9354-45df-b37b-47a138ea6668"), "pv2-power", NodeDataType.Double);
            CreateValueTemplate(factory, SolarTypeGuid, new Guid("f4dda81d-53f8-4f42-b64f-39c3ac51f8a3"), "pv1-voltage", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, SolarTypeGuid, new Guid("ce1e35ad-798e-406d-8fcf-bbbf56995aba"), "pv2-voltage", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, SolarTypeGuid, new Guid("5bbebe03-0bac-4383-8f8b-77608cb96d1d"), "pv1-current", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, SolarTypeGuid, new Guid("ece33b0f-011a-4d64-8eb6-c2be17bad102"), "pv2-current", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, SolarTypeGuid, new Guid("41728aa1-9fdf-4210-ab16-ade4b355a924"), "daily-production", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, SolarTypeGuid, new Guid("2e4b3a1d-989d-47ec-aa2e-5bd343b9a018"), "total-production", NodeDataType.Double, 0, 0.1, false);

            CreateValueTemplate(factory, BatteryTypeGuid, new Guid("710e1ff3-b015-4d09-89bb-b84b269a2726"), "total-battery-charge", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, BatteryTypeGuid, new Guid("9009d64a-f195-4db4-b671-05132dd77092"), "total-battery-discharge", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, BatteryTypeGuid, new Guid("e5f58fc3-c65f-4e0c-acf0-e280f5b9b78d"), "battery-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, BatteryTypeGuid, new Guid("8dde038c-5926-41fe-aa86-ccfd79f1c6cf"), "battery-voltage", NodeDataType.Double, 0, 0.01, false);
            CreateValueTemplate(factory, BatteryTypeGuid, new Guid("90edd528-c9e7-49a4-99e3-9b146ec168c5"), "battery-soc", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryTypeGuid, new Guid("0975c9fb-b38e-48c4-a502-45c6f466ef36"), "battery-current", NodeDataType.Double, 0, 0.01, true);
            CreateValueTemplate(factory, BatteryTypeGuid, new Guid("c6cdcc24-9ed4-4347-bda6-8594d1d4048f"), "battery-temperature", NodeDataType.Double, 1000, 0.1, false);

            CreateValueTemplate(factory, GridTypeGuid, new Guid("7c80923e-e951-4a13-b902-912bd439dfe8"), "grid-status", NodeDataType.Integer);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("489152e0-b689-4c3f-a958-be8ab6957aae"), "total-grid-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("370c2dbf-727d-4fd5-8f0f-811800e05c67"), "grid-voltage-l1", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("5c563be5-de97-4eba-bd6d-9c2cfe1118ca"), "grid-voltage-l2", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("d8fee9c8-2eab-49e1-b1fc-55a96b4e457d"), "grid-voltage-l3", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("18e4a563-8cf6-4599-9487-199958af9176"), "internal-ct-l1-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("ff21cae3-34a1-47c8-8961-62acb6cc3514"), "internal-ct-l2-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("923c2d17-f26a-4fb4-8038-26ef7923ab54"), "internal-ct-l3-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("f26d4f9e-a197-4a23-8165-35f89d7b67d8"), "external-ct-l1-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("0948de51-d2ec-48be-8f5c-b21460130b81"), "external-ct-l2-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("5d9feee5-cd7c-4f7d-a1a7-84851e9246bc"), "external-ct-l3-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("983c90aa-779e-4f13-b498-ad0bb89882de"), "daily-energy-bought", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("ade40731-cb9a-4cac-806e-a45fb0718867"), "total-energy-bought", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("393dd57a-ea7c-4dbe-9152-02080746fe0b"), "daily-energy-sold", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, GridTypeGuid, new Guid("dd89a5c0-e353-4ea7-981f-1fa4dcf27c6c"), "total-energy-sold", NodeDataType.Double, 0, 0.1, false);

            CreateValueTemplate(factory, UploadTypeGuid, new Guid("9d89d408-3ae5-463d-a325-4543d3e990cb"), "total-load-power", NodeDataType.Double);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("dc5d0e96-0ed8-4885-814a-7bd78921e15b"), "load-l1-power", NodeDataType.Double);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("e7a80b24-4203-488c-a722-a5f29fa6432a"), "load-l2-power", NodeDataType.Double);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("15e72d93-bfe9-47c2-a785-dc0d6b1e1835"), "load-l3-power", NodeDataType.Double);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("c8e66a46-547b-4111-8cae-82b00310fdf1"), "load-voltage-l1", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("df065e81-6451-4bc7-a4ab-3d4ae217785e"), "load-voltage-l2", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("2d6233b9-6a2d-4ae8-9622-c9a6b702915f"), "load-voltage-l3", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("b498924b-e6f8-497d-a034-5ed8ca5315a2"), "daily-load-consumption", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("2e0d03c3-5285-4387-8df7-96877a23b7f7"), "total-load-consumption", NodeDataType.Double, 0, 0.1, false);
            CreateValueTemplate(factory, UploadTypeGuid, new Guid("f33530ec-5957-412b-9e16-2fc774548514"), "smartload-enabled-status", NodeDataType.Boolean);

            CreateValueTemplate(factory, InverterTypeGuid, new Guid("0f1d3dc4-1fc4-4901-b1d7-b1bb9638ae56"), "running", NodeDataType.Boolean);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("7c4952c9-b8b6-4cbc-8ad7-156f5ca5ef36"), "total-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("f6e181c7-d5fd-4276-a6ac-ea32748ea6c4"), "current-l1", NodeDataType.Double, 0, 0.01, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("cb9bbbe7-908f-4930-8f19-0ea0337f628f"), "current-l2", NodeDataType.Double, 0, 0.01, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("49c642e6-f3bf-4550-b41d-362ef6de8d92"), "current-l3", NodeDataType.Double, 0, 0.01, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("e9908b8a-c308-481a-b62b-8686316b8738"), "inverter-l1-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("83dd9192-0ca7-4f4c-bbbe-3a2f9e3dcb2a"), "inverter-l2-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("5a22db54-91cd-4831-af66-df08470bcf7b"), "inverter-l3-power", NodeDataType.Double, 0, 1.0, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("392299ca-9eac-4d07-82d3-fac4f6acf43a"), "dc-temperature", NodeDataType.Double, 1000, 0.1, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("195f19be-89eb-47fe-a99c-adf467313500"), "ac-temperature", NodeDataType.Double, 1000, 0.1, true);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("4f61aad8-9a7b-48a6-94e8-a735faa3ea85"), "inverter-id", NodeDataType.String);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("452db178-b597-4f35-9335-37ebd73e469a"), "communcation-board-version", NodeDataType.Integer);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("8f49759e-6917-4959-86c1-2ef4476b2754"), "control-board-version", NodeDataType.Integer);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("eb03774f-2e43-4620-aa2c-11a10065df4d"), "bootloader-version", NodeDataType.Integer);
            CreateValueTemplate(factory, InverterTypeGuid, new Guid("95e66e7a-4f04-4c9d-99f7-a643555f08fe"), "grid-connected-status", NodeDataType.Integer);

            CreateValueTemplate(factory, AlertTypeGuid, new Guid("f2b488e0-c8d5-4ff6-9386-842c8dc78c61"), "alarm-0", NodeDataType.Double);
            CreateValueTemplate(factory, AlertTypeGuid, new Guid("951a18ab-1ce0-4a44-833b-d45c24e4b35c"), "alarm-1", NodeDataType.Double);
            CreateValueTemplate(factory, AlertTypeGuid, new Guid("0c9778b8-4e05-49b8-b5b4-79267e393fb7"), "alarm-2", NodeDataType.Double);
            CreateValueTemplate(factory, AlertTypeGuid, new Guid("9e4997f2-51b7-4045-adf8-666e138b244e"), "alarm-3", NodeDataType.Double);
            CreateValueTemplate(factory, AlertTypeGuid, new Guid("56233e87-d4eb-45e2-9c13-d9987dd5565f"), "alarm-4", NodeDataType.Double);
            CreateValueTemplate(factory, AlertTypeGuid, new Guid("ff65333b-4240-4ac7-92ac-8f788176fd3d"), "alarm-5", NodeDataType.Double);

            // time of use: grid charge flags (172-177, bit 0) are writable, the SOC targets (166-171) read only
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("312f664a-f784-4c94-b1b0-ca50b978046c"), "tou-grid-charge-all", NodeDataType.Boolean, writeable: true);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("d4feb2b0-4dc9-4dd3-85db-626b5428e3bc"), "tou-grid-charge-1", NodeDataType.Boolean, writeable: true);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("2e5537cf-d562-4c0b-8494-76182952fc5e"), "tou-grid-charge-2", NodeDataType.Boolean, writeable: true);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("32ca8b32-66ae-47ca-8165-df72923b29ef"), "tou-grid-charge-3", NodeDataType.Boolean, writeable: true);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("8bdf40e1-3bf0-4ad9-b093-f0229623d584"), "tou-grid-charge-4", NodeDataType.Boolean, writeable: true);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("485dbd12-d96d-46f0-beff-17134421f500"), "tou-grid-charge-5", NodeDataType.Boolean, writeable: true);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("09417020-041c-4402-aee9-a298a596d01f"), "tou-grid-charge-6", NodeDataType.Boolean, writeable: true);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("c9dc5d6d-7aa6-4fa2-92d5-7cee82bf738c"), "tou-soc-1", NodeDataType.Integer);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("57cd5b56-6be9-4648-9d05-72f62ecffb4c"), "tou-soc-2", NodeDataType.Integer);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("44d14b4b-1314-4e44-a629-39080193c829"), "tou-soc-3", NodeDataType.Integer);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("2fcae11f-fd19-4b26-aa38-a31a68a4338b"), "tou-soc-4", NodeDataType.Integer);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("013c293d-f93f-4116-8738-8ceb05a5497b"), "tou-soc-5", NodeDataType.Integer);
            CreateValueTemplate(factory, TimeOfUseTypeGuid, new Guid("1a1a62a2-9172-4a82-a478-e2cf7a6b991c"), "tou-soc-6", NodeDataType.Integer);


        }

        private void CreateValueTemplate(INodeTemplateFactory factory, Guid needsInterfaceGuid, Guid guid, string name, NodeDataType nodeDataType, int offset = 0, double scale = 1.0, bool isSigned = false, bool writeable = false)
        {
            factory.CreateNodeTemplate(guid, $"SOLARMAN.DEVICE.SG03LP3.{name.ToUpperInvariant()}.NAME", $"SOLARMAN.DEVICE.SG03LP3.{name.ToUpperInvariant()}.DESCRIPTION", $"solarman-sg03lp3-{name.ToLowerInvariant()}", needsInterfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, writeable, true, nodeDataType, 1, false, false);
            
            factory.CreatePropertyTemplate(GenerateNewGuid(guid, 1), $"{name}-offset", $"{name}-offset", "offset",
                PropertyTemplateType.Integer, guid, "", false, true, null, offset, 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(guid, 2), $"{name}-scale", $"{name}-scale", "scale",
                PropertyTemplateType.Numeric, guid, "", false, true, null, scale.ToString(CultureInfo.InvariantCulture), 0, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(guid, 3), $"{name}-signed", $"{name}-signed", "signed",
                PropertyTemplateType.Bool, guid, "", false, true, null, isSigned, 0, 0);

        }

        protected static Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[^1] = (byte)(Convert.ToInt32(gu[^1]) + c);

            return new Guid(gu);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new SolarmanDriver(config, new SG04LP3DeviceMap(), new SG043LP3DeviceGroupMap());
        }
    }
}
