using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.Http.Driver.Extraction;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Http.Driver
{
    /// <summary>
    /// Virtual HTTP inputs and outputs: poll a URL and extract values from the response, send HTTP commands.
    /// <code>
    /// HTTP                       allow local host
    /// ├── HTTP input             URL, polling interval, timeout, user, password
    /// │   ├── Online             bool
    /// │   ├── Value / Digital / Text   extraction mode + pattern
    /// └── HTTP output            address, timeout, user, password
    ///     ├── Status             int, HTTP status of the last command (0 = no answer)
    ///     └── Command / Digital command   method, command + body for on and off
    /// </code>
    /// </summary>
    public class HttpDriverFactory : DriverFactory
    {
        public static readonly Guid InputInterface = new("19c84d67-00c0-475d-ba94-154807757078");
        public static readonly Guid InputTemplate = new("28d9da77-7acb-4449-93d0-915b493728dc");
        public static readonly Guid OnlineTemplate = new("40a95bfd-174e-4534-bafa-d3c07a17c521");
        public static readonly Guid InputValueTemplate = new("112e1517-8c7a-4315-aaf4-6e88fc4928a1");
        public static readonly Guid InputDigitalTemplate = new("8cb47f37-b86f-4a3e-af8d-7c0d54eb8292");
        public static readonly Guid InputTextTemplate = new("792baed9-ac05-4956-8541-ec46ebd311f8");

        public static readonly Guid OutputInterface = new("8f3bab09-468f-4643-9c56-93a40d57f4b0");
        public static readonly Guid OutputTemplate = new("72822675-c99d-4881-91f3-30eaf5a56c34");
        public static readonly Guid StatusTemplate = new("02f3d5c1-3dce-4ec4-aa5c-73730a1cdd41");
        public static readonly Guid OutputValueTemplate = new("8a0bc5ab-cb7f-44ee-ba7b-6c8e0167efa6");
        public static readonly Guid OutputDigitalTemplate = new("f4ee3fd5-245c-4705-99f5-7de198d7347f");

        public const string AllowLocalHostKey = "allow_local_host";
        public const string UrlKey = "url";
        public const string AddressKey = "address";
        public const string PollKey = "poll";
        public const string TimeoutKey = "timeout";
        public const string UserKey = "username";
        public const string PasswordKey = "password";
        public const string ModeKey = "mode";
        public const string PatternKey = "pattern";
        public const string MethodKey = "method";
        public const string CommandOnKey = "command_on";
        public const string BodyOnKey = "body_on";
        public const string CommandOffKey = "command_off";
        public const string BodyOffKey = "body_off";
        public const string ContentTypeKey = "content_type";

        public const string DefaultContentType = "application/json";

        public override string DriverName => "Http";

        public static readonly Guid DriverId = new("523bd0a8-984b-49af-a87e-b420727ea169");

        public override Guid DriverGuid => DriverId;

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.http";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new HttpDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "HTTP.NAME", "HTTP.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "HTTP.NAME", "HTTP.DESCRIPTION", "http",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(new Guid("8a3496f6-1b9b-4d0e-9ce6-a43bb373602f"), "HTTP.ALLOW_LOCAL_HOST.NAME", "HTTP.ALLOW_LOCAL_HOST.DESCRIPTION", AllowLocalHostKey,
                PropertyTemplateType.Bool, DriverGuid, "HTTP.CATEGORY.SECURITY", true, false, null, false, 1, 1);

            CreateInput(factory);
            CreateOutput(factory);
        }

        private static void CreateInput(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(InputInterface, "HTTP.INPUT.NAME", "HTTP.INPUT.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(InputTemplate, "HTTP.INPUT.NAME", "HTTP.INPUT.DESCRIPTION", "input", DriverId,
                InputInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("c8a0003a-0567-4b2a-9e37-aa68d8983218"), "HTTP.INPUT.URL.NAME", "HTTP.INPUT.URL.DESCRIPTION", UrlKey,
                PropertyTemplateType.Text, InputTemplate, "HTTP.CATEGORY.REQUEST", true, false, null, "http://", 1, 1);
            factory.CreatePropertyTemplate(new Guid("995edda9-d479-4892-aa92-83d5b85103d4"), "HTTP.INPUT.POLL.NAME", "HTTP.INPUT.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, InputTemplate, "HTTP.CATEGORY.REQUEST", true, false, PropertyHelper.CreateRangeMetaString(10, 604800), 60, 1, 2);
            factory.CreatePropertyTemplate(new Guid("ef4b9371-1f28-46b9-9c65-5b4d7e1eef2a"), "HTTP.TIMEOUT.NAME", "HTTP.TIMEOUT.DESCRIPTION", TimeoutKey,
                PropertyTemplateType.Range, InputTemplate, "HTTP.CATEGORY.REQUEST", true, false, PropertyHelper.CreateRangeMetaString(1, 60), 10, 1, 3);
            factory.CreatePropertyTemplate(new Guid("3c4c004a-804b-4221-9251-8f99d8cea60d"), "HTTP.USERNAME.NAME", "HTTP.USERNAME.DESCRIPTION", UserKey,
                PropertyTemplateType.Text, InputTemplate, "HTTP.CATEGORY.AUTH", true, false, null, "", 2, 1);
            factory.CreatePropertyTemplate(new Guid("44b931a1-b230-4c48-9db9-b00d894fc10d"), "HTTP.PASSWORD.NAME", "HTTP.PASSWORD.DESCRIPTION", PasswordKey,
                PropertyTemplateType.Password, InputTemplate, "HTTP.CATEGORY.AUTH", true, false, null, "", 2, 2);

            factory.CreateNodeTemplate(OnlineTemplate, "HTTP.INPUT.ONLINE.NAME", "HTTP.INPUT.ONLINE.DESCRIPTION", "online", InputInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

            CreateInputCommand(factory, InputValueTemplate, "VALUE", "value", NodeDataType.Double,
                new Guid("997bb9ea-4ebc-4e2f-9b22-22721bfa40f8"), new Guid("f053920d-8b38-4287-aaf5-40eeb9deb2d5"));
            CreateInputCommand(factory, InputDigitalTemplate, "DIGITAL", "digital", NodeDataType.Boolean,
                new Guid("dfd3c67c-38d7-4081-bd3c-6e04c57b3f3d"), new Guid("e2c3bb7f-f642-415d-943e-79ad68ecb2a3"));
            CreateInputCommand(factory, InputTextTemplate, "TEXT", "text", NodeDataType.String,
                new Guid("2e79db15-2153-4d4d-a4f3-1bc5cbba386f"), new Guid("f82a1655-a612-483e-8b7b-b79d4f8abce7"));
        }

        private static void CreateInputCommand(INodeTemplateFactory factory, Guid template, string name, string key, NodeDataType type, Guid modeProperty, Guid patternProperty)
        {
            factory.CreateNodeTemplate(template, $"HTTP.INPUT.{name}.NAME", $"HTTP.INPUT.{name}.DESCRIPTION", key, InputInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, type, int.MaxValue, false);

            factory.CreatePropertyTemplate(modeProperty, "HTTP.MODE.NAME", "HTTP.MODE.DESCRIPTION", ModeKey,
                PropertyTemplateType.Enum, template, "HTTP.CATEGORY.EXTRACT", true, false, PropertyHelper.CreateEnumMetaString(typeof(ExtractMode)), (int)ExtractMode.Json, 1, 1);
            factory.CreatePropertyTemplate(patternProperty, "HTTP.PATTERN.NAME", "HTTP.PATTERN.DESCRIPTION", PatternKey,
                PropertyTemplateType.Text, template, "HTTP.CATEGORY.EXTRACT", true, false, null, "", 1, 2);
        }

        private static void CreateOutput(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(OutputInterface, "HTTP.OUTPUT.NAME", "HTTP.OUTPUT.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(OutputTemplate, "HTTP.OUTPUT.NAME", "HTTP.OUTPUT.DESCRIPTION", "output", DriverId,
                OutputInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("126c0ad6-66b6-427a-bbb3-e2493ab2aa61"), "HTTP.OUTPUT.ADDRESS.NAME", "HTTP.OUTPUT.ADDRESS.DESCRIPTION", AddressKey,
                PropertyTemplateType.Text, OutputTemplate, "HTTP.CATEGORY.REQUEST", true, false, null, "http://", 1, 1);
            factory.CreatePropertyTemplate(new Guid("b6a83236-71aa-4367-8bcb-a7e850e19bab"), "HTTP.TIMEOUT.NAME", "HTTP.TIMEOUT.DESCRIPTION", TimeoutKey,
                PropertyTemplateType.Range, OutputTemplate, "HTTP.CATEGORY.REQUEST", true, false, PropertyHelper.CreateRangeMetaString(1, 60), 10, 1, 2);
            factory.CreatePropertyTemplate(new Guid("2098de16-761b-4d45-88fb-152e517c03c4"), "HTTP.USERNAME.NAME", "HTTP.USERNAME.DESCRIPTION", UserKey,
                PropertyTemplateType.Text, OutputTemplate, "HTTP.CATEGORY.AUTH", true, false, null, "", 2, 1);
            factory.CreatePropertyTemplate(new Guid("0fb59685-bff1-4b52-b447-a82fdb797a66"), "HTTP.PASSWORD.NAME", "HTTP.PASSWORD.DESCRIPTION", PasswordKey,
                PropertyTemplateType.Password, OutputTemplate, "HTTP.CATEGORY.AUTH", true, false, null, "", 2, 2);

            factory.CreateNodeTemplate(StatusTemplate, "HTTP.OUTPUT.STATUS.NAME", "HTTP.OUTPUT.STATUS.DESCRIPTION", "status", OutputInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            CreateOutputCommand(factory, OutputValueTemplate, "COMMAND", "command", NodeDataType.Double, new[]
            {
                new Guid("90d3fabc-4d88-4bb4-a0a4-150f67774fe8"), new Guid("934f676e-6cf2-4a3b-a796-b1554ccf5122"), new Guid("e7c8a009-aeea-4409-a3f3-f6648628ed7e"),
                new Guid("52d519c5-4e0a-4173-b1a6-8abca1036704"), new Guid("4097f366-6a84-41af-afa5-4bc7f98dbc56"), new Guid("2ab46a59-bd5e-4340-b4cc-6d15a1a6cadf")
            });
            CreateOutputCommand(factory, OutputDigitalTemplate, "DIGITAL_COMMAND", "digital_command", NodeDataType.Boolean, new[]
            {
                new Guid("daa7c7f7-6762-438a-8f8c-75d2f0961054"), new Guid("2aa26087-531f-4d16-9a51-a59dffe7f15a"), new Guid("3cb50f31-05d4-4321-a81a-38a42844f8af"),
                new Guid("6ed864a1-aab0-4af5-9280-f72fab8dd222"), new Guid("6a246f8e-2c89-479d-8300-5bc98e54b7bd"), new Guid("f3e19b0d-f14f-4375-bb64-0851b48706c7")
            });
        }

        private static void CreateOutputCommand(INodeTemplateFactory factory, Guid template, string name, string key, NodeDataType type, Guid[] properties)
        {
            factory.CreateNodeTemplate(template, $"HTTP.OUTPUT.{name}.NAME", $"HTTP.OUTPUT.{name}.DESCRIPTION", key, OutputInterface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, true, true, type, int.MaxValue, false);

            factory.CreatePropertyTemplate(properties[0], "HTTP.METHOD.NAME", "HTTP.METHOD.DESCRIPTION", MethodKey,
                PropertyTemplateType.Enum, template, "HTTP.CATEGORY.COMMAND", true, false, PropertyHelper.CreateEnumMetaString(typeof(CommandMethod)), (int)CommandMethod.Get, 1, 1);
            factory.CreatePropertyTemplate(properties[1], "HTTP.COMMAND_ON.NAME", "HTTP.COMMAND_ON.DESCRIPTION", CommandOnKey,
                PropertyTemplateType.Text, template, "HTTP.CATEGORY.COMMAND", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(properties[2], "HTTP.BODY_ON.NAME", "HTTP.BODY_ON.DESCRIPTION", BodyOnKey,
                PropertyTemplateType.Text, template, "HTTP.CATEGORY.COMMAND", true, false, "MULTILINE", "", 1, 3);
            factory.CreatePropertyTemplate(properties[3], "HTTP.COMMAND_OFF.NAME", "HTTP.COMMAND_OFF.DESCRIPTION", CommandOffKey,
                PropertyTemplateType.Text, template, "HTTP.CATEGORY.COMMAND", true, false, null, "", 1, 4);
            factory.CreatePropertyTemplate(properties[4], "HTTP.BODY_OFF.NAME", "HTTP.BODY_OFF.DESCRIPTION", BodyOffKey,
                PropertyTemplateType.Text, template, "HTTP.CATEGORY.COMMAND", true, false, "MULTILINE", "", 1, 5);
            factory.CreatePropertyTemplate(properties[5], "HTTP.CONTENT_TYPE.NAME", "HTTP.CONTENT_TYPE.DESCRIPTION", ContentTypeKey,
                PropertyTemplateType.Text, template, "HTTP.CATEGORY.COMMAND", true, false, null, DefaultContentType, 1, 6);
        }
    }

    public enum CommandMethod
    {
        [EnumName("HTTP.METHOD.GET")]
        Get = 0,
        [EnumName("HTTP.METHOD.POST")]
        Post = 1,
        [EnumName("HTTP.METHOD.PUT")]
        Put = 2
    }
}
