using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Knx.Falcon;
using P3.Driver.Knx.DriverFactory.Attributes;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.P3.Knx.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.Dpt
{
    internal sealed class KnxDptCatalogChild
    {
        public KnxDptCatalogChild(string fieldKey, string guid)
        {
            FieldKey = fieldKey;
            Guid = new Guid(guid);
        }

        public string FieldKey { get; }
        public Guid Guid { get; }

        public string TemplateKey(KnxDptCatalogEntry entry) => $"{entry.Key}-{FieldKey}";
        public string NameKey(KnxDptCatalogEntry entry) => $"{entry.NamePrefix}.{FieldKey.ToUpperInvariant().Replace('-', '_')}.NAME";
    }

    /// <summary>
    /// A datapoint type node template. Single value types are one node, multi-field types are a folder with one child per field.
    /// </summary>
    internal sealed class KnxDptCatalogEntry
    {
        private readonly Func<int, IKnxDptCodec> _codecFactory;

        public KnxDptCatalogEntry(string key, int mainType, string namePrefix, string templateGuid, Func<int, IKnxDptCodec> codecFactory,
            Type subTypeEnum = null, int defaultSubType = 0, int? fixedSubType = null, string interfaceGuid = null,
            params KnxDptCatalogChild[] children)
        {
            Key = key;
            MainType = mainType;
            NamePrefix = namePrefix;
            TemplateGuid = new Guid(templateGuid);
            _codecFactory = codecFactory;
            SubTypeEnum = subTypeEnum;
            DefaultSubType = defaultSubType;
            FixedSubType = fixedSubType;
            InterfaceGuid = interfaceGuid == null ? Guid.Empty : new Guid(interfaceGuid);
            Children = children;
        }

        public string Key { get; }
        public int MainType { get; }
        public string NamePrefix { get; }
        public Guid TemplateGuid { get; }
        public Guid InterfaceGuid { get; }
        public Type SubTypeEnum { get; }
        public int DefaultSubType { get; }
        public int? FixedSubType { get; }
        public IReadOnlyList<KnxDptCatalogChild> Children { get; }

        public bool IsComposite => Children.Count > 0;
        public string NameKey => $"{NamePrefix}.NAME";
        public string DescriptionKey => $"{NamePrefix}.DESCRIPTION";

        public IEnumerable<int> SubTypes => SubTypeEnum == null
            ? FixedSubType.HasValue ? new[] { FixedSubType.Value } : Array.Empty<int>()
            : Enum.GetValues(SubTypeEnum).Cast<int>();

        public NodeDataType DataType => IsComposite ? NodeDataType.NoAttribute : CreateCodec(DefaultSubType).Values.Single().DataType;

        /// <param name="subType">the configured sub type, ignored for types without sub type selection</param>
        public IKnxDptCodec CreateCodec(int subType)
        {
            if (FixedSubType.HasValue)
            {
                return _codecFactory(FixedSubType.Value);
            }

            if (SubTypeEnum == null || !Enum.IsDefined(SubTypeEnum, subType))
            {
                subType = DefaultSubType;
            }

            return _codecFactory(subType);
        }
    }

    /// <summary>
    /// Datapoint types (KNX standard 3.7.2) that are encoded by <see cref="IKnxDptCodec"/> instead of the Falcon SDK.
    /// GUIDs must never change, they identify the node templates in existing configurations.
    /// </summary>
    internal static class KnxDptCatalog
    {
        private static KnxDptCatalogChild C(string key, string guid) => new(key, guid);

        public static readonly IReadOnlyList<KnxDptCatalogEntry> Entries = new List<KnxDptCatalogEntry>
        {
            // ---- single value types ----
            new("knx-dpt4", 4, "KNX.DPT4", "a62b72c8-6128-4822-9cfa-4f9d3efd83b1", KnxDptCodecs.Character, typeof(Dpt4Type), (int)Dpt4Type.Dpt4_001),
            new("knx-dpt12", 12, "KNX.DPT12", "e482fb4f-a66d-4476-906c-f2d4714a1924", _ => KnxDptCodecs.Unsigned32(), typeof(Dpt12Type), (int)Dpt12Type.Dpt12_001),
            new("knx-dpt14", 14, "KNX.DPT14", "b3c69c2b-0ad4-472e-924f-5844febc72d1", _ => KnxDptCodecs.Float32(), typeof(Dpt14Type), (int)Dpt14Type.Dpt14_056),
            new("knx-dpt17", 17, "KNX.DPT17", "65ef52d0-3d0f-4ada-b2fa-c3b7cf5f3a98", _ => KnxDptCodecs.SceneNumber()),
            new("knx-dpt19", 19, "KNX.DPT19", "adce3fbf-2934-40f2-af43-280ccca7d1e3", _ => KnxDptCodecs.DateTime19()),
            new("knx-dpt20", 20, "KNX.DPT20", "e3b56c45-3525-4f3f-b482-4c53bcde07b6", _ => KnxDptCodecs.Enum8(), typeof(Dpt20Type), (int)Dpt20Type.Dpt20_102),
            new("knx-dpt23", 23, "KNX.DPT23", "7849e627-6846-4989-82ed-94e7ac326f2c", _ => KnxDptCodecs.Enum2(), typeof(Dpt23Type), (int)Dpt23Type.Dpt23_001),
            new("knx-dpt24", 24, "KNX.DPT24", "ff15c062-d6fe-476f-ab49-6515e8563e55", _ => KnxDptCodecs.VariableString8859()),
            new("knx-dpt28", 28, "KNX.DPT28", "7cdd5de7-f235-4875-ad3b-6a05422d1803", _ => KnxDptCodecs.VariableStringUtf8()),
            new("knx-dpt29", 29, "KNX.DPT29", "479590d5-b60c-49e6-b6c2-545ed00c6b76", _ => KnxDptCodecs.Signed64(), typeof(Dpt29Type), (int)Dpt29Type.Dpt29_010),
            new("knx-dpt234", 234, "KNX.DPT234", "daa4e815-9696-4f7f-8355-333a28bc3d10", _ => KnxDptCodecs.LanguageCode(), typeof(Dpt234Type), (int)Dpt234Type.Dpt234_001),

            // ---- multi-field types ----
            new("knx-dpt15", 15, "KNX.DPT15", "de7c1caa-a83a-4c60-9515-655aebfea9d4", _ => KnxDptCodecs.AccessData(), interfaceGuid: "059b82d8-c818-4a38-8c60-a3dcc04c385c", children: new[]
            {
                C("code", "3fa66593-856c-4395-ae3d-08620208b89d"), C("error", "5ab53da9-2375-43c2-a861-bfca352cbb32"), C("permission", "54bd80d6-62d5-43d1-b869-deae9f5ca965"), C("direction", "66032072-c8e5-4b4e-b286-bc671bee0617"), C("encrypted", "aa63d803-5be9-4788-8db9-1153d06458e4"), C("index", "9f75f6cf-4368-48f0-a58c-8e2ee9961913")
            }),
            new("knx-dpt18", 18, "KNX.DPT18", "71ad6da5-774a-4199-89fd-5b044733d3f0", _ => KnxDptCodecs.SceneControl(), interfaceGuid: "04a466d2-cd8d-4290-b276-0a500e362348", children: new[]
            {
                C("activate", "7de4c410-2c7a-45aa-a730-13a9b15aecda"), C("learn", "3480cc52-e9be-4a67-be92-7116e393882d")
            }),
            new("knx-dpt21", 21, "KNX.DPT21", "34b518da-7b62-4d7c-8ccc-720c72f79fdc", _ => KnxDptCodecs.BitSet(8), interfaceGuid: "8805db32-0abd-4f68-bc4a-bc1e2cbeb0fe", children: new[]
            {
                C("value", "3fddca44-5c43-4b9f-9d4f-2c82149e6705"), C("bit0", "c816052e-8675-41b4-9804-b712a2a4d77a"), C("bit1", "22bda5ee-300a-4e0f-a87b-cae060bec9eb"), C("bit2", "bf0d69f8-bf17-498b-af51-1958b118c1c7"), C("bit3", "3f0d04b9-cfe6-48a4-a6b8-5dedcadece9e"),
                C("bit4", "f137ba96-7945-4918-a693-70b299d5f1ee"), C("bit5", "f5e1a72a-2b85-420f-9c24-54334fbf5094"), C("bit6", "09dd4b45-a8e2-49e3-8000-74ec50a17346"), C("bit7", "0903d90f-6848-461d-bddd-c2830eb449ac")
            }),
            new("knx-dpt22", 22, "KNX.DPT22", "2a43aff1-dfe8-4db5-814b-4741c9e54b51", _ => KnxDptCodecs.BitSet(16), interfaceGuid: "446a7147-1a38-4cfc-a7b8-e1611ab91ecf", children: new[]
            {
                C("value", "c812cca7-1d20-44bb-bffa-75454354afcf"), C("bit0", "b25b1de5-c6b3-4ca0-94d4-db8efe74fd7d"), C("bit1", "079b9797-4bb8-4963-9355-f643c9194f40"), C("bit2", "04d46830-8e4d-4501-afc4-07e6106198de"), C("bit3", "e34f7145-2b57-4c33-848f-9714ca7ceea8"),
                C("bit4", "74d826dc-bcb9-4d84-9ab2-8fc796614eb8"), C("bit5", "c83ac156-a39d-40d5-be52-80f03cad4fda"), C("bit6", "3b4646c4-94b1-4363-9db8-4e0051d55da7"), C("bit7", "fc760362-a867-43a5-bc8e-944b75aec3d1"), C("bit8", "2713e9d5-e58e-45f2-b5e5-1d7173100a03"),
                C("bit9", "40184bdd-55ec-40f1-ba29-a4ab0ce33337"), C("bit10", "5759fac7-0015-4344-be08-87893275176f"), C("bit11", "4c789cb8-d2bf-4b27-9b63-db8390b4b2c1"), C("bit12", "ef522208-7b75-4c4a-b36c-a3567d8dfe20"), C("bit13", "7c8a58be-28e5-4668-8571-cb9ecf57ee1d"),
                C("bit14", "9e5ec9a2-1625-460f-83f5-8d07fbd12c06"), C("bit15", "ad76a049-2e61-4327-8fac-5e1ce61ea4ff")
            }),
            new("knx-dpt25", 25, "KNX.DPT25", "86d1b44b-b026-4a84-b290-daa5af399828", _ => KnxDptCodecs.DoubleNibble(), interfaceGuid: "1057f523-0778-4dfe-a917-675f900abb31", children: new[]
            {
                C("busy", "27e7751f-8a3f-4bda-af3e-f849738c2a80"), C("nak", "a5d7a2b9-e481-4047-a113-c3fc0f1e24ee")
            }),
            new("knx-dpt26", 26, "KNX.DPT26", "f90452ba-74ea-4f06-b830-5c3db08f29b7", _ => KnxDptCodecs.SceneInfo(), interfaceGuid: "d038b30f-c9e7-4cbc-872a-fbb72720ab5c", children: new[]
            {
                C("scene", "1406d743-c55b-4efe-a147-e20df91a81f5"), C("active", "ae3f1f9e-0bbb-431f-95f7-fccb1f83412f")
            }),
            new("knx-dpt27", 27, "KNX.DPT27", "5b5686d4-4791-42e7-83f0-6f1a6854aad6", _ => KnxDptCodecs.CombinedInfoOnOff(), interfaceGuid: "d97ea4cd-93ca-485c-a96b-8b048addaa2f", children: new[]
            {
                C("output1", "baab6fc4-015e-4ad4-bfa0-337d5eaf067d"), C("output2", "6cf738e0-e627-4278-a77e-11488cbf2650"), C("output3", "8fc58b85-5cca-4ea3-99a2-c26011cbf81a"), C("output4", "c6d0eca1-2a65-418c-a6db-cacd6ca8c2f6"),
                C("output5", "63740d83-143d-43b8-824e-e1cde838d6fb"), C("output6", "aee52810-fec3-4b7e-a74f-194e8e50a435"), C("output7", "66aa4a7d-1a12-44d2-a92e-eeab81a9f158"), C("output8", "44917f17-6103-4991-8c11-2ce916d7f744"),
                C("output9", "f333776b-998f-4986-a6e5-862a5a597240"), C("output10", "984fdd91-8043-4b05-b4ba-cd048a68eb53"), C("output11", "3a09ea5a-7894-4ee0-a66e-9c3b5337aef3"), C("output12", "19534874-13c3-493e-ad36-d8c1948548b9"),
                C("output13", "062083d2-e156-4ab1-8cfb-9890ecb58cfd"), C("output14", "4dc1a7c6-24c0-4181-9a71-97b7d9bb1575"), C("output15", "620fec25-8d12-40f2-b7f3-ce98ee38624e"), C("output16", "a875cb78-d0d5-45d9-84d3-978353f7a9a6")
            }),
            new("knx-dpt206", 206, "KNX.DPT206", "7622c3f1-2a4d-4ed0-8589-7e1469c5ce2b", _ => KnxDptCodecs.TimeDelayAndMode(), typeof(Dpt206Type), (int)Dpt206Type.Dpt206_100,
                interfaceGuid: "548a1560-9c24-4496-8e89-a61fc04d3ff4", children: new[]
                {
                    C("delay", "f32812e1-5d66-48f8-874f-51f76d6f02e2"), C("mode", "a343438d-9ad2-4fa0-b948-a524af102e36")
                }),
            new("knx-dpt207", 207, "KNX.DPT207", "f6891b7f-1963-4411-a3a7-47d16ad7c9ef", _ => KnxDptCodecs.StatusLightingActuator(), interfaceGuid: "6a65f332-6ef7-4550-aad3-d27c8f0fa880", children: new[]
            {
                C("actual-value", "6ac7da50-d65e-4978-a443-4b5cd78de8b8"), C("failure", "a15f95fc-bfac-4445-94e7-58fe6e97c0c3"), C("local-override", "d792b094-a374-4b1a-b778-ff6a25abc94b"), C("dimming", "47493877-f755-4eb0-bf02-655112450e11"),
                C("staircase", "3a7589fe-2999-4dcf-9b30-309d418d1709"), C("night-mode", "d280be94-e31f-4154-bb1d-a8c1113f5f06"), C("forced", "904a8cb9-df90-49b7-a102-97ad93278fb4"), C("locked", "53ed1b57-c9fd-4e16-aef4-b34f1b4b36b7"),
                C("actual-value-valid", "9ba3243d-7d9f-4fa9-9d26-f37b984e92bc")
            }),
            new("knx-dpt217", 217, "KNX.DPT217", "dc40530b-1232-4b60-b4f9-b4d800b56634", _ => KnxDptCodecs.Version(), interfaceGuid: "e6a42b1e-c279-477a-8c00-ef665434f017", children: new[]
            {
                C("magic", "70496df1-08da-4a0c-895b-35ce99f353f2"), C("version", "c8233309-7fe1-4776-80c0-47c9def94cfa"), C("revision", "03b4a5fd-1ccc-40b2-952b-4dcc41d23cc9")
            }),
            new("knx-dpt219", 219, "KNX.DPT219", "47f52313-93ce-4351-bf2f-46b6bdcb3503", _ => KnxDptCodecs.AlarmInfo(), interfaceGuid: "8ec2a2a6-8745-4af0-bb32-f0b130b142bf", children: new[]
            {
                C("log-number", "a6c869d6-be53-4892-a1ce-3f89ae462d45"), C("priority", "672913ee-3733-4efd-b2a4-3fbe97e8e829"), C("application-area", "b834e8fe-03fd-4a76-89ed-2a8b12dfd123"), C("error-class", "a6c0414b-c942-4386-aa73-17913b6f3394"),
                C("errorcode-supported", "e472cae1-2f0c-4146-a51f-c797dc07d62a"), C("alarmtext-supported", "d6d6f607-ebe5-4675-9e0a-0eef8e910876"), C("timestamp-supported", "da9da24b-da06-410e-97fb-5bb18d9c84c6"),
                C("ack-supported", "7c451e6f-251b-4140-a806-840db74068c5"), C("locked", "6746fefc-8f73-44bf-b38b-17ccbdcd188c"), C("alarm-unack", "1a8f3113-b307-4dbd-a86d-9c3f59ad0db6"), C("in-alarm", "5c26110c-45c2-40b4-bcbc-5a55b5000a18")
            }),
            new("knx-dpt222", 222, "KNX.DPT222", "640d6006-a773-4ed4-bb27-017fcc401c5f", _ => KnxDptCodecs.Setpoints3(), typeof(Dpt222Type), (int)Dpt222Type.Dpt222_100,
                interfaceGuid: "aa998bea-ce40-43f6-81ca-d2a759f1fe5a", children: new[]
                {
                    C("comfort", "03dc8934-f955-4932-b5b1-fec06a5f79ea"), C("standby", "f10a9752-0831-41bf-a323-9977e7b5914c"), C("economy", "97a6effa-207e-45b5-867b-268baaab07a9")
                }),
            new("knx-dpt225.001", 225, "KNX.DPT225.001", "cd0e7e6f-e8f0-4134-93af-591e1f77cdd9", _ => KnxDptCodecs.ScalingSpeed(), fixedSubType: 1, interfaceGuid: "d71108f7-9f7a-46fe-b33a-9c45df97a6b9", children: new[]
            {
                C("time", "2bf66e8e-36c9-4899-8b81-3c2a9c9fab76"), C("percent", "f01d5015-521f-4e6d-afdf-4dd1b29de809")
            }),
            new("knx-dpt225.002", 225, "KNX.DPT225.002", "08f6e270-756d-4206-80dc-d42a2bf42e55", _ => KnxDptCodecs.ScalingStepTime(), fixedSubType: 2, interfaceGuid: "d0043012-add4-4c2d-8f83-0f4cd3fd7fe5", children: new[]
            {
                C("time", "77ee2e78-541f-4558-aa22-c75266a64a6d"), C("percent", "b0d49588-7f8d-4dc4-867e-c1f34d62b82c")
            }),
            new("knx-dpt229", 229, "KNX.DPT229", "70795a3c-8aa7-4da7-8311-3613866a6bdc", _ => KnxDptCodecs.MeteringValue(), interfaceGuid: "3f74f120-f359-4e04-98e3-abc097a48c3f", children: new[]
            {
                C("count", "8862039c-bff0-4cb2-b073-c06fd73dfee6"), C("value-information", "6d1671fb-7a49-48eb-b7f4-150669493660"), C("alarm-unack", "5a0cd3ab-b242-41ef-8ebb-c27cd8e13570"), C("in-alarm", "2a1ab13e-0acd-494f-8da6-c27b88d4e109"),
                C("overridden", "bcc95a20-931f-4571-b0be-51b4addc747b"), C("fault", "3eff1cc7-e81b-4bbc-b92c-b7de39e7a17f"), C("out-of-service", "a12c30f9-ad60-49c8-bd2c-4f4613988d19")
            }),
            new("knx-dpt230", 230, "KNX.DPT230", "03c0fa37-be80-4f13-8eb1-b3e31c8b16f4", _ => KnxDptCodecs.MBusAddress(), interfaceGuid: "3c06c2fd-c954-49f6-b081-0e0532077264", children: new[]
            {
                C("manufacturer", "5387c0aa-cb00-48dc-b023-032466060024"), C("ident-number", "d4e1f7dd-02d3-4467-82bd-e48ead505fd6"), C("version", "00a7e581-b9d6-4de9-af5b-42a9824ac9fd"), C("medium", "c8516184-9122-43b2-aafc-dca824194d97")
            }),
            new("knx-dpt232", 232, "KNX.DPT232", "b74641ca-da40-4198-a65c-7881f3270bf0", _ => KnxDptCodecs.ColourRgb(), interfaceGuid: "15209976-1315-4fc2-8630-97c8147d3a55", children: new[]
            {
                C("red", "dcd98cf1-9890-454f-a050-1d9287f354c2"), C("green", "3da4835a-f278-4560-8bb0-f61f6aafb93f"), C("blue", "723f7b8a-d29b-47a2-975e-2c66d7e0f04c"), C("hex", "305db806-f15c-4cec-989c-fdc074faee5b")
            }),
            new("knx-dpt235", 235, "KNX.DPT235", "238f0797-dd37-4978-9183-0e8cff76ec07", _ => KnxDptCodecs.TariffActiveEnergy(), interfaceGuid: "12b13aab-0c0b-45d3-a889-22bd72178eca", children: new[]
            {
                C("energy", "5f1160d2-1330-483c-ab0b-ee278020c7a1"), C("tariff", "fc86a1a8-0137-468a-b060-cad8f92eb41f"), C("energy-invalid", "127ed044-2b68-4d64-b129-cbf5417db6db"), C("tariff-invalid", "67877cd2-98cb-49a7-8225-918e866eada3")
            }),
            new("knx-dpt236", 236, "KNX.DPT236", "b2c6a353-4dce-4ee7-bb99-3fefd3beafc0", _ => KnxDptCodecs.PrioritisedModeControl(), interfaceGuid: "ca7aa72e-e9b6-4451-9d57-40e9e157ffb1", children: new[]
            {
                C("deactivate", "13993ee1-de6d-4f82-a851-8b0e645956ac"), C("priority", "2db01ff8-57e0-49e2-9a9f-04bf54b7df31"), C("mode", "e96de536-ddf8-4009-88d5-a87fb6f5d876")
            }),
            new("knx-dpt237", 237, "KNX.DPT237", "11c43405-caa9-437d-b205-ce1f3b6cf995", _ => KnxDptCodecs.DaliControlGearDiagnostic(), interfaceGuid: "b48b6610-dec7-4370-8a7b-cbc1bc38eba6", children: new[]
            {
                C("converter-error", "cd1b450d-cf06-444c-bcc9-2701f6c595dc"), C("ballast-failure", "f7ac9301-34e2-485d-a8a7-ffac326bc735"), C("lamp-failure", "977ff0ce-411e-4f9d-bcf6-4798bcb9bb55"),
                C("read-response", "5f35a804-e580-4755-9b53-6589f9b7a922"), C("group-address", "3784bcdc-4e6e-4dc4-872d-e789c7326908"), C("address", "78da973a-181a-4368-a383-fdf1849c34dc")
            }),
            new("knx-dpt238", 238, "KNX.DPT238", "6067558d-f515-430b-8498-ec097e9383f3", _ => KnxDptCodecs.DaliDiagnostics(), interfaceGuid: "baa71e00-7b3e-4e36-ba80-3130836e450c", children: new[]
            {
                C("ballast-failure", "f3e0445f-9260-4db3-9d1d-70db8feeab17"), C("lamp-failure", "537ae389-1321-495f-a116-3f2171e36a99"), C("address", "05ad1b2c-b7c3-4f5c-8b72-3893ddded5bb")
            }),
            new("knx-dpt240", 240, "KNX.DPT240", "74a22df6-3fcf-427c-87b4-bf1e90a55b12", _ => KnxDptCodecs.CombinedPosition(), interfaceGuid: "503fa8a9-7f9a-4766-b90a-813bac938f9a", children: new[]
            {
                C("height", "66c3d562-627e-4edf-b88d-99d37bd95797"), C("slats", "f9984207-46d4-4b9b-a622-ddefec658372")
            }),
            new("knx-dpt242", 242, "KNX.DPT242", "d640d69b-3883-4e1f-9902-ac34c2d0d5bb", _ => KnxDptCodecs.ColourXyY(), interfaceGuid: "c6b7c8c7-0af4-47a6-91cc-01b689ab70bc", children: new[]
            {
                C("x", "48e2dc67-b036-443e-9189-1da82bb6653c"), C("y", "281f686a-01cc-4cec-8f55-8cde91d66f85"), C("brightness", "29a9579c-3f0f-47c2-8b94-ede74b7f3a87")
            }),
            new("knx-dpt246", 246, "KNX.DPT246", "f845d0b3-d552-4304-bdba-fafdaaf2866e", _ => KnxDptCodecs.BatteryInfo(), interfaceGuid: "b50fc0ab-4e65-4567-9aaa-9abac85b85e4", children: new[]
            {
                C("fully-charged", "6a9a2d7a-e06e-4ff7-b20c-f5fec351196d"), C("duration-failure", "558646ce-12ec-473e-a907-f845b6862ed4"), C("battery-failure", "7ad016f8-c635-4241-843b-aa27efafb91a"), C("charge-level", "33618275-c1a8-46e5-aa90-9e2c0f8c7f98")
            }),
            new("knx-dpt251", 251, "KNX.DPT251", "fddbfdef-ebe3-4ee0-9a5e-39e0c33e1673", _ => KnxDptCodecs.ColourRgbw(), interfaceGuid: "2a5540e1-a1ab-47c7-ad0d-c90a1f6f0f0d", children: new[]
            {
                C("red", "c360f324-f0e5-4feb-962b-bf49d4b931c3"), C("green", "fe0993f6-50ff-4ff8-b891-b3fb0efe7002"), C("blue", "a66e6d8f-1bf2-4efd-82a7-c0557e4cd728"), C("white", "ded18854-cfc5-40a3-b0dc-aded81fd0889"), C("hex", "c0cef609-8fa0-4cea-80b6-07190d825c73")
            }),
            new("knx-dpt255", 255, "KNX.DPT255", "976e4439-5177-4cd7-ad90-78dee075741d", _ => KnxDptCodecs.GeographicalLocation(), interfaceGuid: "cce7a47b-13a9-4f06-9cbc-96b0944dbe81", children: new[]
            {
                C("longitude", "e0610b66-bb54-406d-a195-0d4a30e55f57"), C("latitude", "6a98ead0-31ae-49e1-a8c6-9f9a7b5d47f6")
            }),
            new("knx-dpt275", 275, "KNX.DPT275", "b969015f-dd83-4b13-a5dc-87ac5679289e", _ => KnxDptCodecs.Setpoints4(), typeof(Dpt275Type), (int)Dpt275Type.Dpt275_100,
                interfaceGuid: "c1052634-89e2-485e-ab32-4e83d8f6981f", children: new[]
                {
                    C("comfort", "a35ec087-8228-4d8e-821b-5ed56d379636"), C("standby", "20a9e9c6-d670-436c-8393-f635003484e2"), C("economy", "10e60f1f-f11e-437c-a5dd-d1290516316d"), C("protection", "48438d65-d358-4d44-8b86-44afb6d3b597")
                }),
        };

        public static KnxDptCatalogEntry GetByKey(string key)
        {
            return Entries.SingleOrDefault(a => a.Key == key);
        }

        /// <summary>
        /// Finds the template for an ETS datapoint type (e.g. 232.600).
        /// </summary>
        public static KnxDptCatalogEntry GetByDpt(int mainType, int subType)
        {
            var candidates = Entries.Where(a => a.MainType == mainType).ToList();
            return candidates.FirstOrDefault(a => a.FixedSubType == subType) ?? candidates.FirstOrDefault(a => !a.FixedSubType.HasValue);
        }

        internal static KnxCodecAttribute CreateAttribute(string key, IDriverContext ctx, KnxDriver driver)
        {
            var entry = GetByKey(key);
            if (entry == null)
            {
                return null;
            }

            return entry.IsComposite ? new KnxCompositeCodecAttribute(ctx, driver, entry) : new KnxCodecAttribute(ctx, driver, entry);
        }

        internal static GroupValue ToGroupValue(IKnxDptCodec codec, byte[] data)
        {
            // values up to 6 bit are sent in the APCI byte
            if (codec.SizeInBit is > 0 and <= 6)
            {
                return new GroupValue(data[0], codec.SizeInBit);
            }

            return new GroupValue(data);
        }
    }
}
