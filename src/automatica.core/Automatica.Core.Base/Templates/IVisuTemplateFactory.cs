using System;

namespace Automatica.Core.Base.Templates
{
    public enum VisuMobileObjectTemplateTypes
    {
        [VisuMobileObjectTemplateType("16780dfd-887a-4a0a-9b2a-4d62ccc32c93")]
        Label = 0,
        [VisuMobileObjectTemplateType("1de48f46-eff4-47bd-be1e-b7e817a66dac")]
        Link = 1,
        [VisuMobileObjectTemplateType("6638ad5b-9a67-4df9-8fcf-7b85e9ceaa74")]
        Slider = 2,
        [VisuMobileObjectTemplateType("c279ff28-8ad6-4803-a0a9-4846357e2e59")]
        ToggleButton = 3,
        [VisuMobileObjectTemplateType("9c4d673f-6415-402b-b04d-506fdd35e1ac")]
        PushButton = 4,
        [VisuMobileObjectTemplateType("481da4c5-9a3a-4c1d-a7b4-eb599b244aeb")]
        NumberBox = 5,
        [VisuMobileObjectTemplateType("7e4029d6-bf42-4354-82b6-e0ece388b94a")]
        WindowMonitor = 6,
        [VisuMobileObjectTemplateType("a39fe74f-e7ae-444a-aef0-f997d56beb1e")]
        Rgba = 7,
        [VisuMobileObjectTemplateType("220da983-0f65-4852-b813-fcbeb331fb16")]
        Chart = 8,
        [VisuMobileObjectTemplateType("0e742e37-5d83-4476-910c-726677b4477f")]
        Gauge = 9,
        [VisuMobileObjectTemplateType("0fc4a94f-1fdb-4ba1-8f61-fd37e0b5757a")]
        ThreeRangeGauge = 10,
        [VisuMobileObjectTemplateType("e3936850-7a0e-4651-9b01-0034e6dabeda")]
        Clock = 11,
        [VisuMobileObjectTemplateType("8b17441f-de01-47c4-be49-5f56ba82440d")]
        Dimmer = 12,
        [VisuMobileObjectTemplateType("82e5f7fc-10ec-4291-9b80-0f9cbee0e74d")]
        MediaPlayer = 13,
        [VisuMobileObjectTemplateType("1ccc646b-b384-4753-aba3-a1f8b840fed8")]
        Shutter = 14,
        [VisuMobileObjectTemplateType("3733855a-667d-4cdd-bd43-6a7246046e4d")]
        Camera = 15,
        [VisuMobileObjectTemplateType("8f0c6d1e-3b7a-4c52-9e41-6a2d5f8b9c03")]
        EnergyFlow = 16,
        // 17..22 and 26 are claimed by other features in progress (room controller, garage, security, ...); keep the numbers unique
        /// <summary>
        /// Status text, icon and color of the status logic (string operations plugin)
        /// </summary>
        [VisuMobileObjectTemplateType("5d2f7a1c-8e34-4b6a-9c1d-2f7e3a9b4c17")]
        Status = 23,
        /// <summary>
        /// Counts per state, objects and activity log of the status monitor logic (string operations plugin)
        /// </summary>
        [VisuMobileObjectTemplateType("6e3a8b2d-9f45-4c7b-8d2e-3a8f4b0c5d18")]
        StatusMonitor = 24,
        /// <summary>
        /// Event log of the tracker logic (string operations plugin)
        /// </summary>
        [VisuMobileObjectTemplateType("7f4b9c3e-0a56-4d8c-9e3f-4b9a5c1d6e19")]
        Tracker = 25,
        /// <summary>
        /// Intelligent room controller (climate logic): current / target temperature, mode, +/- for the comfort temperature
        /// </summary>
        [VisuMobileObjectTemplateType("5b0f3c1e-8d2a-4f6b-9c47-1e2d3a4b5c6d")]
        RoomController = 17,
        /// <summary>
        /// Climate unit (AC unit, fan coil unit): current / target temperature, mode, fan, on/off
        /// </summary>
        [VisuMobileObjectTemplateType("2d6e9a41-7c3b-4f85-a1d2-6b9e0c4f7a28")]
        ClimateUnit = 27,
        /// <summary>
        /// Central climate source (heating/cooling controller, HVAC, AC / fan coil central, mixing valve): read-only state
        /// </summary>
        [VisuMobileObjectTemplateType("3e7fab52-8d4c-4096-b2e3-7cafd15081b9")]
        ClimateCentral = 28
    }

    public interface  IVisuTemplateFactory : IPropertyTemplateFactory
    {
        CreateTemplateCode CreateVisuMobileTemplate(Guid uid, string name, string description, string key, string group, int height, int width, bool isVisibleForUser);
        CreateTemplateCode UpdateMaxMinValues(Guid uid, float? maxHeight, float? maxWidth, float? minHeight, float? minWidth);
    }

    public class VisuMobileObjectTemplateTypeAttribute : Attribute
    {
        private readonly Guid _guid;

        public VisuMobileObjectTemplateTypeAttribute(string guid)
        {
            _guid = new Guid(guid);
        }

        public static Guid GetFromEnum(Enum enumValue)
        {
            var type = enumValue.GetType();
            var memInfo = type.GetMember(enumValue.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(VisuMobileObjectTemplateTypeAttribute), false);

            if (attributes.Length > 0 && attributes[0] is VisuMobileObjectTemplateTypeAttribute attribute)
            {
                return attribute._guid;
            }
            throw new ArgumentException($"enum must provide {nameof(VisuMobileObjectTemplateTypeAttribute)} attribute");
        }

    }
}
