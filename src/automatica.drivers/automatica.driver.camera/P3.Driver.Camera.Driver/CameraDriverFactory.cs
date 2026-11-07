using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Camera.Driver
{
    /// <summary>
    /// IP cameras (RTSP, optionally ONVIF). The driver registers the streams of every camera with go2rtc,
    /// the core serves them to the visu (camera control) through its camera endpoint.
    /// Driver → camera (host, credentials, stream URLs) → online / motion / person
    /// </summary>
    public class CameraDriverFactory : DriverFactory
    {
        public static readonly Guid CameraInterface = new("722f1936-c030-473e-bbf4-437100573c11");
        public static readonly Guid CameraTemplate = new("1c268b43-9b34-4e39-925b-f398f8d87ecb");

        public static readonly Guid OnlineTemplate = new("10e8e734-53ce-4679-ab8a-8eaea6dffc21");
        public static readonly Guid MotionTemplate = new("254519bd-29ef-4611-9581-642378a4332c");
        public static readonly Guid PersonTemplate = new("f9dcb799-bc97-496b-a76d-cfd334bb7c89");

        /// <summary>
        /// The data points of a camera, created with the camera by the scan
        /// </summary>
        public static readonly Guid[] CameraValueTemplates = { OnlineTemplate, MotionTemplate, PersonTemplate };

        public const string Go2RtcUrlKey = "go2rtc_url";
        public const string Go2RtcUsernameKey = "go2rtc_username";
        public const string Go2RtcPasswordKey = "go2rtc_password";
        public const string PollKey = "poll";

        public const string HostKey = "host";
        public const string OnvifPortKey = "onvif_port";
        public const string UsernameKey = "username";
        public const string PasswordKey = "password";
        public const string UseOnvifKey = "use_onvif";
        public const string StreamMainKey = "stream_main";
        public const string StreamSubKey = "stream_sub";
        public const string SnapshotUrlKey = "snapshot_url";
        public const string EventsKey = "events";

        public const string DefaultGo2RtcUrl = "http://127.0.0.1:1984";

        public override string DriverName => "Camera";

        public override Guid DriverGuid => new("ebe3fa72-1f18-4ece-a1f8-0172bff79c13");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.camera";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new CameraDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "CAMERA.NAME", "CAMERA.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "CAMERA.NAME", "CAMERA.DESCRIPTION", "camera-driver",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(new Guid("15d39001-523b-41b2-9db8-75600829fa46"), "CAMERA.GO2RTC_URL.NAME", "CAMERA.GO2RTC_URL.DESCRIPTION", Go2RtcUrlKey,
                PropertyTemplateType.Text, DriverGuid, "CAMERA.CATEGORY.GO2RTC", true, false, null, DefaultGo2RtcUrl, 1, 1);
            factory.CreatePropertyTemplate(new Guid("2f0856db-6e5b-4702-a474-cc38760206b6"), "CAMERA.GO2RTC_USERNAME.NAME", "CAMERA.GO2RTC_USERNAME.DESCRIPTION", Go2RtcUsernameKey,
                PropertyTemplateType.Text, DriverGuid, "CAMERA.CATEGORY.GO2RTC", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("a54406c2-eb67-471d-9aa7-3eccf9581b1f"), "CAMERA.GO2RTC_PASSWORD.NAME", "CAMERA.GO2RTC_PASSWORD.DESCRIPTION", Go2RtcPasswordKey,
                PropertyTemplateType.Password, DriverGuid, "CAMERA.CATEGORY.GO2RTC", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("f77f8ce0-2b40-481e-83e6-cdf9d181da6a"), "CAMERA.POLL.NAME", "CAMERA.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "CAMERA.CATEGORY.GO2RTC", true, false, PropertyHelper.CreateRangeMetaString(10, 600), 30, 1, 4);
            factory.CreatePropertyTemplate(new Guid("046c3cfc-d098-4a3a-a5a2-d33e6f836823"), "CAMERA.SCAN.NAME", "CAMERA.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            CreateCamera(factory);
        }

        private void CreateCamera(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(CameraInterface, "CAMERA.CAMERA.NAME", "CAMERA.CAMERA.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(CameraTemplate, "CAMERA.CAMERA.NAME", "CAMERA.CAMERA.DESCRIPTION", "camera", DriverGuid,
                CameraInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("f818e6b2-f338-48fb-880d-c5f5f2688d05"), "CAMERA.CAMERA.HOST.NAME", "CAMERA.CAMERA.HOST.DESCRIPTION", HostKey,
                PropertyTemplateType.Text, CameraTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("d0a5f21b-71fa-4a1f-87d4-9d5678b41f6f"), "CAMERA.CAMERA.ONVIF_PORT.NAME", "CAMERA.CAMERA.ONVIF_PORT.DESCRIPTION", OnvifPortKey,
                PropertyTemplateType.Integer, CameraTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, 80, 1, 2);
            factory.CreatePropertyTemplate(new Guid("346621fe-96c6-4771-8de9-1ba9c60ff4f2"), "CAMERA.CAMERA.USERNAME.NAME", "CAMERA.CAMERA.USERNAME.DESCRIPTION", UsernameKey,
                PropertyTemplateType.Text, CameraTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("8016e945-d540-4f38-bfde-16bfdb159987"), "CAMERA.CAMERA.PASSWORD.NAME", "CAMERA.CAMERA.PASSWORD.DESCRIPTION", PasswordKey,
                PropertyTemplateType.Password, CameraTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 4);

            factory.CreatePropertyTemplate(new Guid("232d683c-ac77-4881-afa2-ceb997ffc399"), "CAMERA.CAMERA.USE_ONVIF.NAME", "CAMERA.CAMERA.USE_ONVIF.DESCRIPTION", UseOnvifKey,
                PropertyTemplateType.Bool, CameraTemplate, "CAMERA.CATEGORY.STREAMS", true, false, null, true, 2, 1);
            factory.CreatePropertyTemplate(new Guid("8125140b-a868-47bf-8d3d-bd58ad970325"), "CAMERA.CAMERA.STREAM_MAIN.NAME", "CAMERA.CAMERA.STREAM_MAIN.DESCRIPTION", StreamMainKey,
                PropertyTemplateType.Text, CameraTemplate, "CAMERA.CATEGORY.STREAMS", true, false, null, "", 2, 2);
            factory.CreatePropertyTemplate(new Guid("9c846a05-2ebc-44fd-a94e-15dec9713a0e"), "CAMERA.CAMERA.STREAM_SUB.NAME", "CAMERA.CAMERA.STREAM_SUB.DESCRIPTION", StreamSubKey,
                PropertyTemplateType.Text, CameraTemplate, "CAMERA.CATEGORY.STREAMS", true, false, null, "", 2, 3);
            factory.CreatePropertyTemplate(new Guid("3285837c-87d7-46b0-83a6-c3e00971f8ba"), "CAMERA.CAMERA.SNAPSHOT_URL.NAME", "CAMERA.CAMERA.SNAPSHOT_URL.DESCRIPTION", SnapshotUrlKey,
                PropertyTemplateType.Text, CameraTemplate, "CAMERA.CATEGORY.STREAMS", true, false, null, "", 2, 4);
            factory.CreatePropertyTemplate(new Guid("c2e22d23-e275-4f4c-83e1-c045c7c14011"), "CAMERA.CAMERA.EVENTS.NAME", "CAMERA.CAMERA.EVENTS.DESCRIPTION", EventsKey,
                PropertyTemplateType.Bool, CameraTemplate, "CAMERA.CATEGORY.STREAMS", true, false, null, true, 2, 5);

            factory.CreateNodeTemplate(OnlineTemplate, "CAMERA.CAMERA.ONLINE.NAME", "CAMERA.CAMERA.ONLINE.DESCRIPTION", "online", CameraInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(MotionTemplate, "CAMERA.CAMERA.MOTION.NAME", "CAMERA.CAMERA.MOTION.DESCRIPTION", "motion", CameraInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(PersonTemplate, "CAMERA.CAMERA.PERSON.NAME", "CAMERA.CAMERA.PERSON.DESCRIPTION", "person", CameraInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);
        }
    }
}
