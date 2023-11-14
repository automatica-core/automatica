using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.SonosDriverFactory
{
    public class SonosDriverFactory : DriverFactory
    {
        public static readonly Guid BusId = new("df404af7-a487-4742-805a-0b871d1b5447");

        public static Guid SonosDeviceGuid = new("d6daedf4-9dd6-48c9-8f5d-8dd56e4e0748");
        public static Guid SonosDeviceInterfaceGuid = new("ba2a0906-6789-42b3-a460-e8d624dde38e");

        public static Guid PlayGuid = new("6a482ed1-33ed-47de-b58e-d850652d4e40");
        public static Guid PauseGuid = new("48b1eeba-13e5-4300-bea3-e26836c64cb8");
        public static Guid VolumeGuid = new("5285e217-7793-4aef-91d2-230bbe61b5c7");
        public static Guid NextTrack = new("3e22c6b8-c557-4be7-b671-cd627855b05d");
        public static Guid PreviousTrack = new("1f60a282-e649-43a5-af7f-9b82c6b41a80");
        public static Guid SetTuneInRadio = new("77947332-cd89-4765-b79b-b64ce4fa23cf");
        public static Guid SetTuneInRadioAndPlay = new("fc5c92c1-8011-4e8d-96fb-25e2accf10b9");
        public static Guid StatusGuid = new ("d317d318-3c65-46cc-b0ab-f894c2dc51d0");

        public static Guid SetMediaUrl = new("96a53a5a-b833-49a8-89e4-b55548a50b78");
        public static Guid SetMediaUrlAndPlay = new("df859942-8dc2-47c8-8086-9c592b9cc366");


        public const string IdAddressPropertyKey = "device-id";
        public const string UseFixedIpAddressPropertyKey = "use-fixed-ip";
        public const string IpAddressPropertyKey = "ip";

        public override string DriverName => "sonos";
        public override Guid DriverGuid => BusId;
        public override string ImageName => "automaticacore/plugin-p3.driver.sonos"; 

        public override Version DriverVersion => new(1, 2, 0, 3);

        
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "SONOS.NAME", "SONOS.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "SONOS.NAME", "SONOS.DESCRIPTION", "sonos", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("293865c5-8aac-4695-9cc0-52eb79129ee3"), "SONOS.SCAN.NAME", "SONOS.SCAN.DESCRIPTION",
                "scan", PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0,
                0);

            var gwInterface = SonosDeviceInterfaceGuid;
            factory.CreateInterfaceType(gwInterface, "SONOS.DEVICE.NAME", "SONOS.DEVICE.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(SonosDeviceGuid, "SONOS.DEVICE.NAME", "SONOS.DEVICE.NAME", "sonos-device", DriverGuid, gwInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("594be0ff-63c4-4e46-9cc8-37ff7ccf80a2"), "SONOS.DEVICE.ID.NAME",
                "SONOS.DEVICE.ID.DESCRIPTION", IdAddressPropertyKey, PropertyTemplateType.Text, SonosDeviceGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, "", null, 1, 0);

            factory.CreatePropertyTemplate(new Guid("d103c3c5-0937-4596-a94e-caefeb66eda6"), "SONOS.DEVICE.USE_FIXED_IP.NAME",
                "SONOS.DEVICE.USE_FIXED_IP.DESCRIPTION", UseFixedIpAddressPropertyKey, PropertyTemplateType.Bool, SonosDeviceGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, "", null, 1, 0);

            factory.CreatePropertyTemplate(new Guid("5a37d2f6-b081-4637-b72c-3fb40dcf45b7"), "SONOS.DEVICE.IP.NAME",
                "SONOS.DEVICE.IP.DESCRIPTION", IpAddressPropertyKey, PropertyTemplateType.Ip, SonosDeviceGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, "", null, 1, 0);


            CreateAction(factory, PlayGuid, "play", true, true, NodeDataType.Boolean);
            CreateAction(factory, PauseGuid, "pause", true, true, NodeDataType.Boolean);
            CreateAction(factory, VolumeGuid, "set_volume", true, true, NodeDataType.Integer);
            CreateAction(factory, NextTrack, "next", true, false, NodeDataType.Boolean);
            CreateAction(factory, PreviousTrack, "previous", true, false, NodeDataType.Boolean);

            CreateAction(factory, SetTuneInRadio, "set_tune_in", true, false, NodeDataType.String);
            CreateAction(factory, SetTuneInRadioAndPlay, "set_tune_in_play", true, false, NodeDataType.String);


            CreateAction(factory, SetMediaUrl, "set_media_url", true, false, NodeDataType.String);
            CreateAction(factory, SetMediaUrlAndPlay, "set_media_url_play", true, false, NodeDataType.String);

            CreateCurrentStatusItems(factory);
        }

        private void CreateCurrentStatusItems(INodeTemplateFactory factory)
        {
            var statusGuid = StatusGuid;
            factory.CreateInterfaceType(statusGuid, "SONOS.DEVICE.STATUS.NAME", "SONOS.DEVICE.STATUS.DESCRIPTION", int.MaxValue, 1, false);
            factory.CreateNodeTemplate(statusGuid, "SONOS.DEVICE.STATUS.NAME", "SONOS.DEVICE.STATUS.DESCRIPTION", "sonos-status", SonosDeviceInterfaceGuid,
                statusGuid, true, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(new Guid("aea0dd95-0dd5-49ce-8a2d-606eb1355c7a"), "SONOS.DEVICE.STATUS.TITLE.NAME", "SONOS.DEVICE.STATUS.TITLE.DESCRIPTION", "sonos-status-title", statusGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, false, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(new Guid("840af2d3-d326-4d72-bb1e-da7c69a4f5a2"), "SONOS.DEVICE.STATUS.CREATOR.NAME", "SONOS.DEVICE.STATUS.CREATOR.DESCRIPTION", "sonos-status-creator", statusGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, false, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(new Guid("cbc6244e-9e12-4178-bd0f-b7102482c2b1"), "SONOS.DEVICE.STATUS.ALBUM.NAME", "SONOS.DEVICE.STATUS.ALBUM.DESCRIPTION", "sonos-status-album", statusGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, false, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(new Guid("8ae25092-6de4-4fcb-9038-adc1271cc038"), "SONOS.DEVICE.STATUS.ALBUM_ART_URI.NAME", "SONOS.DEVICE.STATUS.ALBUM_ART_URI.DESCRIPTION", "sonos-status-album-art-uri", statusGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, false, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(new Guid("9c34f7ff-2356-45b4-b680-df40c914b42c"), "SONOS.DEVICE.STATUS.CLASS.NAME", "SONOS.DEVICE.STATUS.CLASS.DESCRIPTION", "sonos-status-class", statusGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, false, NodeDataType.String, 1, false);

            factory.CreateNodeTemplate(new Guid("9f49e650-32f2-4694-b763-94c33b1ddf8c"), "SONOS.DEVICE.STATUS.DURATION.NAME", "SONOS.DEVICE.STATUS.DURATION.DESCRIPTION", "sonos-status-duration", statusGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, false, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(new Guid("a4f4af10-cff6-4b90-99f1-ac5c2963e158"), "SONOS.DEVICE.STATUS.RELATIVE_TIME.NAME", "SONOS.DEVICE.STATUS.RELATIVE_TIME.DESCRIPTION", "sonos-status-relative-time", statusGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, false, NodeDataType.String, 1, false);
        }

        private void CreateAction(INodeTemplateFactory factory, Guid guid, string name, bool writeAble, bool readAble, NodeDataType dataType)
        {
            factory.CreateNodeTemplate(guid, $"SONOS.DEVICE.{name.ToUpperInvariant()}.NAME",
                $"SONOS.DEVICE.{name.ToUpperInvariant()}.DESCRIPTION", name.ToLowerInvariant(), SonosDeviceInterfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, readAble, true, writeAble, true,
                dataType, 1, false);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new SonosDriver(config);
        }

        public override void AfterSave(NodeInstance instance)
        {
            // do nothing
        }
    }
}
