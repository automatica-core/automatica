using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Base.Visu;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.Visu
{
    public class VisuTempInit : IVisualisationFactory
    {
        public void Initialize(AutomaticaContext database, IConfiguration config)
        {
            var factory = new VisuMobileTemplateFactory(NullLogger.Instance, database, config);
            factory.SetFactory(new VisuMobileFactory());

            var label = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label);
            factory.CreateVisuMobileTemplate(label, "VISU.OBJECT.LABEL.NAME", "VISU.OBJECT.LABEL.DESCRIPTION", "label", "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            AddCommonProperty(label, factory);
            AddTextProperty(label, factory);

            factory.CreatePropertyTemplate(new Guid("d7d8dd9d-3c45-44b6-9bbd-d7d00825ebdb"), "VISU.APPEARANCE.NODE_VALUE.NAME", "VISU.APPEARANCE.NODE_VALUE.DESCRIPTION", "nodeInstance",
                PropertyTemplateType.NodeInstance, label, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, null, 1, 2);

            var link = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Link);
            factory.CreateVisuMobileTemplate(link, "VISU.OBJECT.LINK.NAME", "VISU.OBJECT.LINK.DESCRIPTION", "link",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);
            factory.CreatePropertyTemplate(new Guid("bea56e63-47e9-4050-9333-e54de0cdcfb6"), "VISU.APPEARANCE.LINK.NAME", "VISU.APPEARANCE.LINK.DESCRIPTION", "link",
                PropertyTemplateType.VisuMobilePage, link, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, null, 1, 2);

            factory.CreatePropertyTemplate(new Guid("fb20bc35-3b9a-4b70-8483-ba79c5cd8cf0"), "VISU.APPEARANCE.AREA_LINK.NAME", "VISU.APPEARANCE.AREA_LINK.DESCRIPTION", "area_link",
                PropertyTemplateType.AreaInstanceLink, link, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, null, 1, 2);

            AddTextProperty(link, factory);
            AddCommonProperty(link, factory);

            var slider = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Slider);
            factory.CreateVisuMobileTemplate(slider, "VISU.OBJECT.SLIDER.NAME", "VISU.OBJECT.SLIDER.DESCRIPTION", "slider",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            AddCommonProperty(slider, factory);
            AddTextProperty(slider, factory);

            factory.CreatePropertyTemplate(new Guid("ac6a8a89-6361-48c1-b1d9-82439eeff7ea"), "VISU.APPEARANCE.MIN.NAME", "VISU.APPEARANCE.MIN.DESCRIPTION", "min",
                PropertyTemplateType.Integer, slider, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, 0, 1, 2);

            factory.CreatePropertyTemplate(new Guid("55c3b706-62c4-4e9e-b066-798c893a8e9f"), "VISU.APPEARANCE.MAX.NAME", "VISU.APPEARANCE.MAX.DESCRIPTION", "max",
                PropertyTemplateType.Integer, slider, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, 100, 1, 2);

            factory.CreatePropertyTemplate(new Guid("e51cbe53-6301-4b11-a523-864f453b7b24"), "VISU.APPEARANCE.NODE_VALUE.NAME", "VISU.APPEARANCE.NODE_VALUE.DESCRIPTION", "nodeInstance",
                PropertyTemplateType.NodeInstance, slider, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, null, 1, 2);

            factory.CreatePropertyTemplate(new Guid("1b6b41c9-2494-42d4-893e-63d0d9fec2e7"), "VISU.APPEARANCE.READONLY.NAME", "VISU.APPEARANCE.READONLY.DESCRIPTION", "readonly",
                PropertyTemplateType.Bool, slider, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "false", 100, 1, 2);

            AddToggleButton(factory);
            AddNumberBox(factory);


            AddWindowMonitor(factory);
            AddRgbControl(factory);

            AddChartControl(factory);
            AddGaugeControl(factory);

            AddClockControl(factory);
            AddMediaControl(factory);

            AddPushButton(factory);

            AddSlider(factory);

            var dimmer = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Dimmer);
            factory.CreateVisuMobileTemplate(dimmer, "VISU.OBJECT.DIMMER.NAME", "VISU.OBJECT.DIMMER.DESCRIPTION",
                "dimmer", "VISU.CATEGORY.COMMON.NAME", 1, 1, true);
        }

        private void AddMediaControl(VisuMobileTemplateFactory factory)
        {
            var mediaControl = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.MediaPlayer);
            factory.CreateVisuMobileTemplate(mediaControl, "VISU.OBJECT.MEDIA_PLAYER.NAME", "VISU.OBJECT.MEDIA_PLAYER.DESCRIPTION", "media_player",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            factory.UpdateMaxMinValues(mediaControl, 1, 1, 1, 1);
            AddCommonProperty(mediaControl, factory);
            
            factory.CreatePropertyTemplate(mediaControl, "radio_list", "radio_list", "radio_list",
                PropertyTemplateType.DropDown, mediaControl, "COMMON", false, false, "Ö3,s8007;FM4,s8235;OE1,s8514;Antenne Österreich,s310727;Rock Antenne Österreich,s306882; Energy Österreich,s921", "s8007", 0, 0);
        }

        private void AddClockControl(VisuMobileTemplateFactory factory)
        {
            var clock = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Clock);
            factory.CreateVisuMobileTemplate(clock, "VISU.OBJECT.CLOCK.NAME", "VISU.OBJECT.CLOCK.DESCRIPTION", "clock",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            factory.UpdateMaxMinValues(clock, 1, 1, 1, 1);
            AddCommonProperty(clock, factory);
        }

        private enum GaugeType
        {
            Linear = 0,
            Circular = 1
        }

        private void AddGaugeControl(VisuMobileTemplateFactory factory)
        {
            var chart = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Gauge);
            factory.CreateVisuMobileTemplate(chart, "VISU.OBJECT.GAUGE.NAME", "VISU.OBJECT.GAUGE.DESCRIPTION", "gauge",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            var threeRangeGauge = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.ThreeRangeGauge);
            factory.CreateVisuMobileTemplate(threeRangeGauge, "VISU.OBJECT.GAUGE.NAME", "VISU.OBJECT.GAUGE.DESCRIPTION", "three-range-gauge",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            factory.CreatePropertyTemplate(new Guid("85e6e721-d45b-479c-ab44-6672d2f772dc"), "VISU.APPEARANCE.NODE_VALUE.NAME", "VISU.APPEARANCE.NODE_VALUE.DESCRIPTION", "nodeInstance",
                PropertyTemplateType.NodeInstance, chart, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("258fad47-e750-4e3e-9620-9d1afabfef1a"), "VISU.APPEARANCE.GAUGE_TYPE.NAME",
                "VISU.APPEARANCE.GAUGE_TYPE.DESCRIPTION", "gauge_type", PropertyTemplateType.Enum, chart,
                "VISU.CATEGORY.APPEARANCE.NAME", true, false, PropertyHelper.CreateEnumMetaString(typeof(GaugeType)),
                (int)GaugeType.Circular, 0, 0);


            factory.CreatePropertyTemplate(new Guid("da0c1047-bfd0-4095-b0ac-781a3808e162"), "VISU.APPEARANCE.SCALE_START.NAME",
                "VISU.APPEARANCE.SCALE_START.DESCRIPTION", "scale_start", PropertyTemplateType.Integer, chart,
                "VISU.CATEGORY.APPEARANCE.NAME", true, false, null,
                0, 0, 10);

            factory.CreatePropertyTemplate(new Guid("d1d5ce5f-94e2-48c1-a065-89557f12dca4"), "VISU.APPEARANCE.SCALE_END.NAME",
                "VISU.APPEARANCE.SCALE_END.DESCRIPTION", "scale_end", PropertyTemplateType.Integer, chart,
                "VISU.CATEGORY.APPEARANCE.NAME", true, false, null,
                100, 0, 11);

            factory.CreatePropertyTemplate(new Guid("81bbbbc1-2b14-4e44-9f6d-ee7ebacc6dcf"), "VISU.APPEARANCE.TICKS.NAME",
                "VISU.APPEARANCE.TICKS.DESCRIPTION", "ticks", PropertyTemplateType.Numeric, chart,
                "VISU.CATEGORY.APPEARANCE.NAME", true, false, null,
                1, 0, 12);



            AddCommonProperty(chart, factory);
            AddTextProperty(chart, factory);
        }

        private void AddChartControl(VisuMobileTemplateFactory factory)
        {
            var chart = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Chart);
            factory.CreateVisuMobileTemplate(chart, "VISU.OBJECT.CHART.NAME", "VISU.OBJECT.CHART.DESCRIPTION", "chart",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            factory.CreatePropertyTemplate(new Guid("6c3f4a8c-4fe1-48cc-af1a-1b5a23d0bdb3"), "VISU.APPEARANCE.NODE_VALUE.NAME", "VISU.APPEARANCE.NODE_VALUE.DESCRIPTION", "nodeInstance",
                PropertyTemplateType.NodeInstance, chart, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, null, 1, 1);


            AddCommonProperty(chart, factory);
            AddTextProperty(chart, factory);
        }

        private void AddRgbControl(VisuMobileTemplateFactory factory)
        {
            var rgba = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Rgba);
            factory.CreateVisuMobileTemplate(rgba, "VISU.OBJECT.RGBA.NAME", "VISU.OBJECT.RGBA.DESCRIPTION", "rgba",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            factory.CreatePropertyTemplate(new Guid("6c3f4a8c-4fe1-48cc-af1a-1b5a23d0bdb3"), "VISU.APPEARANCE.NODE_VALUE.NAME", "VISU.APPEARANCE.NODE_VALUE.DESCRIPTION", "nodeInstance",
                PropertyTemplateType.NodeInstance, rgba, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("3eef3ccb-34f9-4e2c-b804-0236ee32d184"), "VISU.APPEARANCE.READONLY.NAME", "VISU.APPEARANCE.READONLY.DESCRIPTION", "readonly",
                PropertyTemplateType.Bool, rgba, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "", false, 1, 2);

            AddCommonProperty(rgba, factory);
            AddTextProperty(rgba, factory);
        }

        private void AddWindowMonitor(VisuMobileTemplateFactory factory)
        {
            var monitor = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.WindowMonitor);
            factory.CreateVisuMobileTemplate(monitor, "VISU.OBJECT.WINDOW_MONITOR.NAME", "VISU.OBJECT.WINDOW_MONITOR.DESCRIPTION", "window-monitor",
                "VISU.CATEGORY.PRIVATE.NAME", 1, 1, false);

        }

        private void AddSlider(VisuMobileTemplateFactory factory)
        {
            var toggleButton = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Slider);
            factory.CreateVisuMobileTemplate(toggleButton, "VISU.OBJECT.SLIDER.NAME",
                "VISU.OBJECT.SLIDER.DESCRIPTION", "slider",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

        }

        private void AddToggleButton(VisuMobileTemplateFactory factory)
        {
            var toggleButton = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.ToggleButton);
            factory.CreateVisuMobileTemplate(toggleButton, "VISU.OBJECT.TOGGLE_BUTTON.NAME", "VISU.OBJECT.TOGGLE_BUTTON.DESCRIPTION", "toggle-button",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            factory.CreatePropertyTemplate(new Guid("8238bcc7-4396-464a-ae8b-d615582fb97b"), "VISU.APPEARANCE.NODE_VALUE.NAME", "VISU.APPEARANCE.NODE_VALUE.DESCRIPTION", "nodeInstance",
                PropertyTemplateType.NodeInstance, toggleButton, "VISU.CATEGORY.VALUE.NAME", true, false, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("85d1ccd7-36e6-4129-b34b-f0e9100ac1ee"), "VISU.APPEARANCE.NODE_STATE_VALUE.NAME", "VISU.APPEARANCE.NODE_STATE_VALUE.DESCRIPTION", "nodeInstanceState",
                PropertyTemplateType.NodeInstance, toggleButton, "VISU.CATEGORY.VALUE.NAME", true, false, null, null, 1, 2);

            factory.CreatePropertyTemplate(new Guid("3eef3ccb-34f9-4e2c-b804-0236ee32d184"), "VISU.APPEARANCE.READONLY.NAME", "VISU.APPEARANCE.READONLY.DESCRIPTION", "readonly",
                PropertyTemplateType.Bool, toggleButton, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "", false, 2, 1);

            factory.CreatePropertyTemplate(new Guid("f487195e-a447-42ca-919d-6fd36e27eddc"), "VISU.APPEARANCE.TOGGLE.ON.NAME", "VISU.APPEARANCE.TOGGLE.ON.DESCRIPTION", "toggle_on_text",
                PropertyTemplateType.Text, toggleButton, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "", "On", 2, 2);

            factory.CreatePropertyTemplate(new Guid("336fb367-593e-4165-a3dd-283182e28f18"), "VISU.APPEARANCE.TOGGLE.OFF.NAME", "VISU.APPEARANCE.TOGGLE.OFF.DESCRIPTION", "toggle_off_text",
                PropertyTemplateType.Text, toggleButton, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "", "Off", 2, 3);

            AddCommonProperty(toggleButton, factory);
            AddTextProperty(toggleButton, factory);
        }

        enum PushButtonBehaviour
        {
            SendValue = 0,
            Switch = 1,
            SendTrue = 2,
            SendFalse = 3
        }
        private void AddPushButton(VisuMobileTemplateFactory factory)
        {
            var pushButton = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.PushButton);
            factory.CreateVisuMobileTemplate(pushButton, "VISU.OBJECT.PUSH_BUTTON.NAME", "VISU.OBJECT.PUSH_BUTTON.DESCRIPTION", "push-button",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            factory.CreatePropertyTemplate(new Guid("a4339ad8-09bc-46e7-a395-0e1e79bbeab7"), "VISU.APPEARANCE.NODE_VALUE.NAME", "VISU.APPEARANCE.NODE_VALUE.DESCRIPTION", "nodeInstance",
                PropertyTemplateType.NodeInstance, pushButton, "VISU.CATEGORY.VALUE.NAME", true, false, null, null, 1, 1);
            
            factory.CreatePropertyTemplate(new Guid("90461b81-8c04-409f-928a-37bb7eb4e1ea"), "VISU.APPEARANCE.SEND_VALUE.NAME", "VISU.APPEARANCE.SEND_VALUE.DESCRIPTION", "send_value",
                PropertyTemplateType.Text, pushButton, "VISU.CATEGORY.VALUE.NAME", true, false, "", true, 1, 2);

            factory.CreatePropertyTemplate(new Guid("e2509320-c72f-410b-bda0-6bd9459cb50f"), "VISU.APPEARANCE.READONLY.NAME", "VISU.APPEARANCE.READONLY.DESCRIPTION", "readonly",
                PropertyTemplateType.Bool, pushButton, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "", false, 1, 1);

            factory.CreatePropertyTemplate(new Guid("9c1f9103-f580-47ba-ad30-b21f0a93ac5a"), "VISU.APPEARANCE.TOGGLE.ON.NAME", "VISU.APPEARANCE.TOGGLE.ON.DESCRIPTION", "toggle_on_text",
                PropertyTemplateType.Text, pushButton, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "", "On", 1, 2);

            factory.CreatePropertyTemplate(new Guid("1dba9926-dba7-4cd1-a7c3-5cf9b9197bf0"), "VISU.APPEARANCE.TOGGLE.OFF.NAME", "VISU.APPEARANCE.TOGGLE.OFF.DESCRIPTION", "toggle_off_text",
                PropertyTemplateType.Text, pushButton, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "", "Off", 1, 3);

            factory.CreatePropertyTemplate(new Guid("2c05f69d-dff1-4110-9a4f-7b6aa886c00d"), "VISU.APPEARANCE.BEHAVIOUR.NAME", "VISU.APPEARANCE.BEHAVIOUR.DESCRIPTION", "behaviour",
                PropertyTemplateType.Enum, pushButton, "VISU.CATEGORY.APPEARANCE.NAME", true, false, PropertyHelper.CreateEnumMetaString(typeof(PushButtonBehaviour)), PushButtonBehaviour.SendTrue, 1, 4);

            

            AddCommonProperty(pushButton, factory);
            AddTextProperty(pushButton, factory);
        }

        private void AddNumberBox(VisuMobileTemplateFactory factory)
        {
            var numberBox = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.NumberBox);
            factory.CreateVisuMobileTemplate(numberBox, "VISU.OBJECT.NUMBER_BOX.NAME", "VISU.OBJECT.NUMBER_BOX.DESCRIPTION", "number-box",
                "VISU.CATEGORY.COMMON.NAME", 1, 1, true);

            factory.CreatePropertyTemplate(new Guid("a38973e4-0a2f-4849-9919-505142a49400"), "VISU.APPEARANCE.NODE_VALUE.NAME", "VISU.APPEARANCE.NODE_VALUE.DESCRIPTION", "nodeInstance",
                PropertyTemplateType.NodeInstance, numberBox, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("137b65c1-31aa-4a10-a9d0-8bdbc621eb7f"), "VISU.APPEARANCE.READONLY.NAME", "VISU.APPEARANCE.READONLY.DESCRIPTION", "readonly",
                PropertyTemplateType.Bool, numberBox, "VISU.CATEGORY.APPEARANCE.NAME", true, false, "", false, 1, 2);

            factory.CreatePropertyTemplate(new Guid("da8681a7-d7dc-4553-8a01-4db66b30af3f"), "VISU.APPEARANCE.MIN.NAME", "VISU.APPEARANCE.MIN.DESCRIPTION", "min",
                PropertyTemplateType.Integer, numberBox, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, 0, 1, 3);

            factory.CreatePropertyTemplate(new Guid("f70f0342-6ec6-4b99-b39f-197553f17bf9"), "VISU.APPEARANCE.MAX.NAME", "VISU.APPEARANCE.MAX.DESCRIPTION", "max",
                PropertyTemplateType.Integer, numberBox, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, 10, 1, 4);

            factory.CreatePropertyTemplate(new Guid("e4d43570-8794-466e-b318-7a4abe17af9c"), "VISU.APPEARANCE.STEP.NAME", "VISU.APPEARANCE.STEP.DESCRIPTION", "step",
                PropertyTemplateType.Numeric, numberBox, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, 1, 1, 5);

            AddCommonProperty(numberBox, factory);
            AddTextProperty(numberBox, factory);

        }

        private void AddCommonProperty(Guid parent, VisuMobileTemplateFactory factory)
        {
            factory.CreatePropertyTemplate(GenerateNewGuid(parent, 1, 2), "VISU.APPEARANCE.BACKGROUND_COLOR.NAME", "VISU.APPEARANCE.BACKGROUND_COLOR.DESCRIPTION", "background_color",
                PropertyTemplateType.Color, parent, "VISU.CATEGORY.APPEARANCE.NAME", true, false, null, "rgba(255, 255, 255, 1)", 1, 2);
        }

        private void AddTextProperty(Guid parent, VisuMobileTemplateFactory factory)
        {
            factory.CreatePropertyTemplate(GenerateNewGuid(parent, 1), "VISU.APPEARANCE.TEXT.NAME", "VISU.APPEARANCE.TEXT.DESCRIPTION", "text",
                PropertyTemplateType.Text, parent, "VISU.CATEGORY.TEXT.NAME", true, false, null, null, 2, 0);

            factory.CreatePropertyTemplate(GenerateNewGuid(parent, 2), "VISU.APPEARANCE.TEXT_SIZE.NAME", "VISU.APPEARANCE.TEXT_SIZE.DESCRIPTION", "text_size",
                PropertyTemplateType.Integer, parent, "VISU.CATEGORY.TEXT.NAME", true, false, null, 20, 2, 1);

            factory.CreatePropertyTemplate(GenerateNewGuid(parent, 3), "VISU.APPEARANCE.FOREGROUND.NAME", "VISU.APPEARANCE.FOREGROUND.DESCRIPTION", "foreground_color",
                PropertyTemplateType.Color, parent, "VISU.CATEGORY.TEXT.NAME", true, false, null, "rgba(0, 0, 0, 1)", 2, 2);

            factory.CreatePropertyTemplate(GenerateNewGuid(parent, 4), "VISU.APPEARANCE.TEXT_BREAK.NAME", "VISU.APPEARANCE.TEXT_BREAK.DESCRIPTION", "text_break",
                PropertyTemplateType.Bool, parent, "VISU.CATEGORY.TEXT.NAME", true, false, null, true, 3, 1);

            factory.CreatePropertyTemplate(GenerateNewGuid(parent, 5), "VISU.APPEARANCE.UNIT.NAME", "VISU.APPEARANCE.UNIT.DESCRIPTION", "unit",
                PropertyTemplateType.Text, parent, "VISU.CATEGORY.TEXT.NAME", true, false, null, "", 4, 1);
        }

        private Guid GenerateNewGuid(Guid guid, int c, int index = 1)
        {
            byte[] gu = guid.ToByteArray();

            gu[^index] = (byte)(Convert.ToInt32(gu[^index]) + c);

            return new Guid(gu);
        }

    }
}
