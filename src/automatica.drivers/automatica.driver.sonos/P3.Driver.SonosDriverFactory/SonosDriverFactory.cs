using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.SonosDriverFactory
{
    public class SonosDriverFactory : DriverFactory
    {
        public static readonly Guid BusId = new Guid("df404af7-a487-4742-805a-0b871d1b5447");

        public static Guid SonosDeviceGuid = new Guid("d6daedf4-9dd6-48c9-8f5d-8dd56e4e0748");
        public static Guid SonosDeviceInterfaceGuid = new Guid("ba2a0906-6789-42b3-a460-e8d624dde38e");

        public static Guid PlayGuid = new Guid("6a482ed1-33ed-47de-b58e-d850652d4e40");
        public static Guid PauseGuid = new Guid("48b1eeba-13e5-4300-bea3-e26836c64cb8");
        public static Guid SetVolumeGuid = new Guid("5285e217-7793-4aef-91d2-230bbe61b5c7");
        public static Guid NextTrack = new Guid("3e22c6b8-c557-4be7-b671-cd627855b05d");
        public static Guid SetTuneInRadio = new Guid("77947332-cd89-4765-b79b-b64ce4fa23cf");
        public static Guid SetTuneInRadioAndPlay = new Guid("fc5c92c1-8011-4e8d-96fb-25e2accf10b9");


        public const string IdAddressPropertyKey = "device-id";
        public const string UseFixedIpAddressPropertyKey = "use-fixed-ip";
        public const string IpAddressPropertyKey = "ip";

        public override string DriverName => "sonos";
        public override Guid DriverGuid => BusId;

        public override Version DriverVersion => new Version(0, 3, 0, 4);

        
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "SONOS.NAME", "SONOS.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "SONOS.NAME", "SONOS.DESCRIPTION", "sonos", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("293865c5-8aac-4695-9cc0-52eb79129ee3"), "SONOS.SCAN.NAME", "SONOS.SCAN.DESCRIPTION",
                "scan", PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0,
                0);

            var gwInterface = SonosDeviceInterfaceGuid;
            factory.CreateInterfaceType(gwInterface, "SONOS.DEVICE.NAME", "SONOS.DEVICE.DESCRIPTION", int.MaxValue, int.MaxValue, true);
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
            CreateAction(factory, SetVolumeGuid, "set_volume", true, true, NodeDataType.Integer);
            CreateAction(factory, NextTrack, "next", true, true, NodeDataType.Boolean);

            CreateAction(factory, SetTuneInRadio, "set_tune_in", true, true, NodeDataType.Integer);
            CreateAction(factory, SetTuneInRadioAndPlay, "set_tune_in_play", true, true, NodeDataType.Integer);

        }

        private void CreateAction(INodeTemplateFactory factory, Guid guid, string name, bool writeAble, bool readAble, NodeDataType dataType)
        {
            factory.CreateNodeTemplate(guid, $"SONOS.{name.ToUpperInvariant()}.NAME",
                $"SONOS.{name.ToUpperInvariant()}.DESCRIPTION", name.ToLowerInvariant(), SonosDeviceInterfaceGuid,
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
