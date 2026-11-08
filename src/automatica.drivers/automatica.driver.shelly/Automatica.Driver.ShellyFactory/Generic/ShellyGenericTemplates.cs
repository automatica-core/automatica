#nullable enable annotations
using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Driver.ShellyFactory.Catalog;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace Automatica.Driver.ShellyFactory.Generic
{
    /// <summary>
    /// Templates of the generic devices: one Gen1 and one Gen2+ device template, one template per component type and data point.
    /// </summary>
    internal static class ShellyGenericTemplates
    {
        public static readonly Guid Gen1DeviceTemplate = new("4a8f0e57-1c63-4b2d-9e7a-0d5b3c9f6a21");
        public static readonly Guid Gen1DeviceInterface = new("e7b2c1d4-58f9-4a06-b3c7-91d2e8f4a650");
        public static readonly Guid RpcDeviceTemplate = new("9c1e6b38-7d24-4f5a-a0e9-3b8d6c2f1e74");
        public static readonly Guid RpcDeviceInterface = new("2d6f9a1c-b43e-4e87-8c15-f0a7d3b2e968");

        public const string Gen1DeviceKey = "shelly-gen1";
        public const string RpcDeviceKey = "shelly-rpc";

        public const string ComponentIdKey = "shelly-component-id";
        public const string PollingIntervalKey = "polling-interval";
        public const string UserNameKey = "shelly-username";
        public const string PasswordKey = "shelly-password";
        public const string ModelKey = "shelly-model";
        public const string GenerationKey = "shelly-generation";
        public const string SleepingKey = "shelly-sleeping";
        public const string ModeKey = "shelly-mode";
        public const string CoIoTDescriptionKey = "shelly-coiot-description";

        // driver (root node) properties
        public const string ScanAddressesKey = "shelly-scan-addresses";
        public const string DefaultUserNameKey = "shelly-default-username";
        public const string DefaultPasswordKey = "shelly-default-password";
        public const string CoIoTEnabledKey = "shelly-coiot";
        public const string OutboundWebSocketPortKey = "shelly-outbound-ws-port";
        public const string LearnKey = "shelly-learn";

        public const int Gen1DefaultPolling = 5000;
        public const int RpcDefaultPolling = 60000;

        public static IEnumerable<ShellyComponentType> AllComponents
        {
            get
            {
                foreach (var c in ShellyGen1Catalog.All)
                {
                    yield return c;
                }

                foreach (var c in ShellyRpcCatalog.All)
                {
                    yield return c;
                }
            }
        }

        public static void InitDriverProperties(INodeTemplateFactory factory, Guid driverGuid)
        {
            factory.CreatePropertyTemplate(new Guid("0c4b7e92-3f1a-4d68-b5e0-a2c9f7d3e186"), "SHELLY.PROPERTIES.SCAN_ADDRESSES.NAME", "SHELLY.PROPERTIES.SCAN_ADDRESSES.DESCRIPTION",
                ScanAddressesKey, PropertyTemplateType.Text, driverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("6e1d3a58-92c4-4b7f-8d06-c5f2a9e1b437"), "SHELLY.PROPERTIES.DEFAULT_USERNAME.NAME", "SHELLY.PROPERTIES.DEFAULT_USERNAME.DESCRIPTION",
                DefaultUserNameKey, PropertyTemplateType.Text, driverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, null, "admin", 1, 3);
            factory.CreatePropertyTemplate(new Guid("b38f5c17-6a2e-4e93-9f41-d7c0a8e2b65f"), "SHELLY.PROPERTIES.DEFAULT_PASSWORD.NAME", "SHELLY.PROPERTIES.DEFAULT_PASSWORD.DESCRIPTION",
                DefaultPasswordKey, PropertyTemplateType.Password, driverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, null, "", 1, 4);
            factory.CreatePropertyTemplate(new Guid("d92a6e04-1b7c-4f35-a8e9-63f0c4b1d7a2"), "SHELLY.PROPERTIES.LEARN.NAME", "SHELLY.PROPERTIES.LEARN.DESCRIPTION",
                LearnKey, PropertyTemplateType.LearnMode, driverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, null, null, 1, 5);
            factory.CreatePropertyTemplate(new Guid("47c0e3b9-d85a-4a16-b2f7-e9a1c6d04f38"), "SHELLY.PROPERTIES.COIOT.NAME", "SHELLY.PROPERTIES.COIOT.DESCRIPTION",
                CoIoTEnabledKey, PropertyTemplateType.Bool, driverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, true, 2, 1);
            factory.CreatePropertyTemplate(new Guid("a15d8f2e-7b39-4c04-9e6a-f2d4b0c7e893"), "SHELLY.PROPERTIES.OUTBOUND_WS_PORT.NAME", "SHELLY.PROPERTIES.OUTBOUND_WS_PORT.DESCRIPTION",
                OutboundWebSocketPortKey, PropertyTemplateType.Integer, driverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, 0, 2, 2);
        }

        public static void Init(INodeTemplateFactory factory, Guid driverGuid)
        {
            InitDevice(factory, driverGuid, Gen1DeviceTemplate, Gen1DeviceInterface, Gen1DeviceKey, "SHELLY.GEN1_DEVICE", true);
            InitDevice(factory, driverGuid, RpcDeviceTemplate, RpcDeviceInterface, RpcDeviceKey, "SHELLY.RPC_DEVICE", false);

            foreach (var component in ShellyGen1Catalog.All)
            {
                InitComponent(factory, component, Gen1DeviceInterface);
            }

            foreach (var component in ShellyRpcCatalog.All)
            {
                InitComponent(factory, component, RpcDeviceInterface);
            }
        }

        private static void InitDevice(INodeTemplateFactory factory, Guid driverGuid, Guid template, Guid deviceInterface, string key, string i18n, bool gen1)
        {
            factory.CreateInterfaceType(deviceInterface, $"{i18n}.NAME", $"{i18n}.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(template, $"{i18n}.NAME", $"{i18n}.DESCRIPTION", key, driverGuid, deviceInterface,
                false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            Property(factory, template, ShellyFactory.DeviceIdPropertyKey, "SHELLY.PROPERTIES.ID", PropertyTemplateType.Text, "", 1, 1);
            Property(factory, template, ShellyFactory.DeviceIpPropertyKey, "SHELLY.PROPERTIES.IP", PropertyTemplateType.Text, "", 1, 2);
            if (gen1)
            {
                Property(factory, template, UserNameKey, "SHELLY.PROPERTIES.USERNAME", PropertyTemplateType.Text, "", 1, 3);
            }
            Property(factory, template, PasswordKey, "SHELLY.PROPERTIES.PASSWORD", PropertyTemplateType.Password, "", 1, 4);
            Property(factory, template, PollingIntervalKey, "SHELLY.PROPERTIES.POLLING_INTERVAL", PropertyTemplateType.Integer,
                gen1 ? Gen1DefaultPolling : RpcDefaultPolling, 1, 5);
            Property(factory, template, SleepingKey, "SHELLY.PROPERTIES.SLEEPING", PropertyTemplateType.Bool, false, 1, 6);

            Property(factory, template, ModelKey, "SHELLY.PROPERTIES.MODEL", PropertyTemplateType.Text, "", 2, 1, isReadonly: true);
            Property(factory, template, GenerationKey, "SHELLY.PROPERTIES.GENERATION", PropertyTemplateType.Integer, gen1 ? 1 : 2, 2, 2, isReadonly: true);
            if (gen1)
            {
                Property(factory, template, ModeKey, "SHELLY.PROPERTIES.MODE", PropertyTemplateType.Text, "", 2, 3, isReadonly: true);
                // CoIoT description of a sleeping device, read at the scan while it was awake
                Property(factory, template, CoIoTDescriptionKey, "SHELLY.PROPERTIES.COIOT_DESCRIPTION", PropertyTemplateType.Text, "", 2, 4,
                    isReadonly: true, isVisible: false);
            }
        }

        private static void Property(INodeTemplateFactory factory, Guid template, string key, string i18n, PropertyTemplateType type, object defaultValue,
            int groupOrder, int order, bool isReadonly = false, bool isVisible = true)
        {
            factory.CreatePropertyTemplate(ShellyGuid.Create(template, $"property:{key}"), $"{i18n}.NAME", $"{i18n}.DESCRIPTION", key, type, template,
                "COMMON.CATEGORY.ADDRESS", isVisible, isReadonly, null, defaultValue, groupOrder, order);
        }

        private static void InitComponent(INodeTemplateFactory factory, ShellyComponentType component, Guid deviceInterface)
        {
            var i18n = $"SHELLY.COMPONENT.{component.I18N}";
            factory.CreateInterfaceType(component.Interface, $"{i18n}.NAME", $"{i18n}.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(component.Template, $"{i18n}.NAME", $"{i18n}.DESCRIPTION", ComponentKey(component), deviceInterface, component.Interface,
                false, false, true, false, true, NodeDataType.NoAttribute, component.HasId ? int.MaxValue : 1, false);

            if (component.HasId)
            {
                factory.CreatePropertyTemplate(ShellyGuid.Create(component.Template, $"property:{ComponentIdKey}"), "SHELLY.PROPERTIES.COMPONENT_ID.NAME",
                    "SHELLY.PROPERTIES.COMPONENT_ID.DESCRIPTION", ComponentIdKey, PropertyTemplateType.Integer, component.Template, "COMMON.CATEGORY.ADDRESS",
                    true, false, null, 0, 1, 1);
            }

            foreach (var dp in component.DataPoints)
            {
                factory.CreateNodeTemplate(dp.Template, $"SHELLY.DP.{dp.I18N}.NAME", $"SHELLY.DP.{dp.I18N}.DESCRIPTION", dp.Key, component.Interface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, dp.IsReadable, true, dp.IsWriteable, true, dp.Type, 1, false);
            }
        }

        public static string ComponentKey(ShellyComponentType component) =>
            $"shelly-{(component.Api == ShellyApi.Gen1 ? "gen1" : "rpc")}-{(component.Type.Length == 0 ? "sensors" : component.Type)}";
    }
}
