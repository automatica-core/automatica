using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.NetworkPresence.Driver
{
    /// <summary>
    /// Tracks which phones are connected to the local network and derives who is at home.
    /// Driver → person (scan lists the phones seen on the network) → phone (MAC address)
    /// </summary>
    public class NetworkPresenceDriverFactory : DriverFactory
    {
        public static readonly Guid PersonInterface = new("3f7b628f-4ef3-48ad-9b36-89cbb6bb925f");
        public static readonly Guid PersonTemplate = new("ef5d6c86-0cc7-4bea-85f2-4ffab24bee44");
        public static readonly Guid DeviceInterface = new("8efff22b-8205-4de7-9b4c-6ed5e4fa6ed3");
        public static readonly Guid DeviceTemplate = new("08b8c37c-59ff-425f-8732-eed008859faf");

        /// <summary>
        /// The data points of a phone, created with the phone by the scan
        /// </summary>
        public static readonly Guid[] DeviceValueTemplates =
        {
            new("99d5e696-6eeb-4a8d-8f4d-ab9c81d16dd8"), // connected
            new("7a12c282-80f8-42b6-91a4-c42884ce5a72"), // ip
            new("93f2484a-8fdb-4390-b661-0d957abc3cd3"), // last_seen
        };

        public const string SubnetsKey = "subnets";
        public const string PollKey = "poll";
        public const string AwayTimeoutKey = "away_timeout";
        public const string FullScanKey = "full_scan";
        public const string MacKey = "mac";

        public override string DriverName => "NetworkPresence";

        public override Guid DriverGuid => new("dfbac560-176d-4d2b-9752-7859de28556a");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.networkpresence";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new NetworkPresenceDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "NETWORK_PRESENCE.NAME", "NETWORK_PRESENCE.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "NETWORK_PRESENCE.NAME", "NETWORK_PRESENCE.DESCRIPTION", "network-presence",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(new Guid("a3d28676-d03f-4190-8753-db8943225f6e"), "NETWORK_PRESENCE.SUBNETS.NAME", "NETWORK_PRESENCE.SUBNETS.DESCRIPTION", SubnetsKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("fbf29af5-bca5-4f94-83a7-cb9c62c07c4a"), "NETWORK_PRESENCE.POLL.NAME", "NETWORK_PRESENCE.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(10, 600), 30, 1, 2);
            factory.CreatePropertyTemplate(new Guid("54f89f85-7451-4b12-ae87-3b42bcb3d7f4"), "NETWORK_PRESENCE.AWAY_TIMEOUT.NAME", "NETWORK_PRESENCE.AWAY_TIMEOUT.DESCRIPTION", AwayTimeoutKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 120), 10, 1, 3);
            factory.CreatePropertyTemplate(new Guid("91034c27-785b-4665-b457-3883a74987cf"), "NETWORK_PRESENCE.FULL_SCAN.NAME", "NETWORK_PRESENCE.FULL_SCAN.DESCRIPTION", FullScanKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 60), 5, 1, 4);

            factory.CreateNodeTemplate(new Guid("3b2a6c04-2f77-48f1-8596-17a297c776f5"), "NETWORK_PRESENCE.ANYONE_HOME.NAME", "NETWORK_PRESENCE.ANYONE_HOME.DESCRIPTION", "anyone_home", DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(new Guid("2b7703e8-a178-4fd3-b4fa-be3b0ffc6440"), "NETWORK_PRESENCE.PEOPLE_HOME.NAME", "NETWORK_PRESENCE.PEOPLE_HOME.DESCRIPTION", "people_home", DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            CreatePerson(factory);
            CreateDevice(factory);
        }

        private void CreatePerson(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(PersonInterface, "NETWORK_PRESENCE.PERSON.NAME", "NETWORK_PRESENCE.PERSON.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(PersonTemplate, "NETWORK_PRESENCE.PERSON.NAME", "NETWORK_PRESENCE.PERSON.DESCRIPTION", "person", DriverGuid,
                PersonInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("549f246b-a2d5-4128-95a8-c8039e7e1772"), "NETWORK_PRESENCE.PERSON.SCAN.NAME", "NETWORK_PRESENCE.PERSON.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, PersonTemplate, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            factory.CreateNodeTemplate(new Guid("9eeb2722-c307-4179-8443-a332b0d74705"), "NETWORK_PRESENCE.PERSON.HOME.NAME", "NETWORK_PRESENCE.PERSON.HOME.DESCRIPTION", "home", PersonInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(new Guid("c0752dcc-6d27-4c19-b933-69b19c7a0ca8"), "NETWORK_PRESENCE.PERSON.LAST_SEEN.NAME", "NETWORK_PRESENCE.PERSON.LAST_SEEN.DESCRIPTION", "last_seen", PersonInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
            factory.CreateNodeTemplate(new Guid("ffbd476d-5c1a-4069-9084-b5d674617be5"), "NETWORK_PRESENCE.PERSON.DEVICES_HOME.NAME", "NETWORK_PRESENCE.PERSON.DEVICES_HOME.DESCRIPTION", "devices_home", PersonInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
        }

        private void CreateDevice(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DeviceInterface, "NETWORK_PRESENCE.DEVICE.NAME", "NETWORK_PRESENCE.DEVICE.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(DeviceTemplate, "NETWORK_PRESENCE.DEVICE.NAME", "NETWORK_PRESENCE.DEVICE.DESCRIPTION", "device", PersonInterface,
                DeviceInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("7afcc026-b22f-445a-9213-8a8c295a63e5"), "NETWORK_PRESENCE.DEVICE.MAC.NAME", "NETWORK_PRESENCE.DEVICE.MAC.DESCRIPTION", MacKey,
                PropertyTemplateType.Text, DeviceTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);

            factory.CreateNodeTemplate(DeviceValueTemplates[0], "NETWORK_PRESENCE.DEVICE.CONNECTED.NAME", "NETWORK_PRESENCE.DEVICE.CONNECTED.DESCRIPTION", "connected", DeviceInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(DeviceValueTemplates[1], "NETWORK_PRESENCE.DEVICE.IP.NAME", "NETWORK_PRESENCE.DEVICE.IP.DESCRIPTION", "ip", DeviceInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(DeviceValueTemplates[2], "NETWORK_PRESENCE.DEVICE.LAST_SEEN.NAME", "NETWORK_PRESENCE.DEVICE.LAST_SEEN.DESCRIPTION", "last_seen", DeviceInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
        }
    }
}
