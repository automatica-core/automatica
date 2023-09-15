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
        Char = 8,
        [VisuMobileObjectTemplateType("0e742e37-5d83-4476-910c-726677b4477f")]
        Gauge = 9,
        [VisuMobileObjectTemplateType("0fc4a94f-1fdb-4ba1-8f61-fd37e0b5757a")]
        ThreeRangeGauge = 10,
        [VisuMobileObjectTemplateType("e3936850-7a0e-4651-9b01-0034e6dabeda")]
        Clock = 11,
        [VisuMobileObjectTemplateType("8b17441f-de01-47c4-be49-5f56ba82440d")]
        Dimmer = 12,
        [VisuMobileObjectTemplateType("82e5f7fc-10ec-4291-9b80-0f9cbee0e74d")]
        MediaPlayer = 13
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
