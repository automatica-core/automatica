using System;
using System.Linq;
using Automatica.Core.Shading.Definition;
using Xunit;

namespace Automatica.Core.Tests.Shading
{
    public class ShadingDefinitionTests
    {
        [Fact]
        public void Settings_RoundTrip_WithMixedSources()
        {
            var node = Guid.NewGuid();
            var clouds = Guid.NewGuid();
            var settings = new ShadingSettingsDefinition
            {
                Brightness = new Signal
                {
                    Sources =
                    {
                        new DatapointSource { Node = node, Factor = 1000, StaleAfterMinutes = 10 },
                        new CalculatedSource { CloudCover = Signal.FromDatapoint(clouds) }
                    }
                },
                ManualOverride = ManualOverrideMode.Duration
            };

            var json = ShadingJson.Serialize(settings);
            Assert.Contains("\"type\":\"datapoint\"", json);
            Assert.Contains("\"type\":\"calculated\"", json);
            Assert.Contains("\"manualOverride\":\"duration\"", json);

            var parsed = ShadingJson.Parse<ShadingSettingsDefinition>(json);
            var datapoint = Assert.IsType<DatapointSource>(parsed.Brightness.Sources[0]);
            Assert.Equal(node, datapoint.Node);
            Assert.Equal(1000, datapoint.Factor);
            var calculated = Assert.IsType<CalculatedSource>(parsed.Brightness.Sources[1]);
            Assert.Equal(clouds, Assert.IsType<DatapointSource>(calculated.CloudCover.Sources.Single()).Node);
        }

        [Fact]
        public void Parse_EmptyOrPartial_UsesDefaults()
        {
            var settings = ShadingJson.Parse<ShadingSettingsDefinition>(null);
            Assert.IsType<CalculatedSource>(settings.SunAzimuth.Sources.Single());
            Assert.Empty(settings.Wind.Sources);

            settings = ShadingJson.Parse<ShadingSettingsDefinition>("{\"wind\":null,\"thresholds\":null}");
            Assert.NotNull(settings.Wind);
            Assert.Equal(20000, settings.Thresholds.OnLux);

            var blind = ShadingJson.Parse<BlindDefinition>("{\"type\":\"venetian\",\"slats\":null}");
            Assert.Equal(BlindType.Venetian, blind.Type);
            Assert.NotNull(blind.Slats);
        }

        [Fact]
        public void Parse_Invalid_Throws()
        {
            Assert.Throws<ShadingDefinitionException>(() => ShadingJson.Parse<ShadingSettingsDefinition>("{\"brightness\":{\"sources\":[{\"type\":\"unknown\"}]}}"));
            Assert.Throws<ShadingDefinitionException>(() => ShadingJson.Parse<BlindDefinition>("not json"));
        }

        [Fact]
        public void Validate_Settings()
        {
            Assert.Empty(ShadingValidator.Validate(new ShadingSettingsDefinition()));

            var settings = new ShadingSettingsDefinition
            {
                Thresholds = new SunThresholds { OnLux = 10000, OffLux = 20000 },
                Wind = new Signal { Sources = { new CalculatedSource() } },
                Rain = new Signal { Sources = { new DatapointSource() } }
            };
            var codes = ShadingValidator.Validate(settings).Select(a => a.Code).ToList();
            Assert.Contains("OFF_ABOVE_ON", codes);
            Assert.Contains("NOT_CALCULABLE", codes);
            Assert.Contains("DATAPOINT_REQUIRED", codes);
        }

        [Fact]
        public void Validate_FacadeAndBlind()
        {
            Assert.Empty(ShadingValidator.Validate("South", 180, new FacadeDefinition()));
            Assert.Contains(ShadingValidator.Validate("", 360, new FacadeDefinition { MinElevation = 50, MaxElevation = 40 }), a => a.Path == "azimuth");
            Assert.Contains(ShadingValidator.Validate("x", 0, new FacadeDefinition { MinElevation = 50, MaxElevation = 40 }), a => a.Code == "MIN_ABOVE_MAX");
            Assert.Empty(ShadingValidator.Validate("Roof", 180, new FacadeDefinition { Inclination = 0 }));
            Assert.Contains(ShadingValidator.Validate("Roof", 180, new FacadeDefinition { Inclination = 95 }), a => a.Path == "inclination");
            Assert.Contains(ShadingValidator.Validate(new BlindDefinition { Inclination = -1 }), a => a.Path == "inclination" && a.Code == "OUT_OF_RANGE");

            Assert.Empty(ShadingValidator.Validate(new BlindDefinition { Type = BlindType.Venetian }));
            var problems = ShadingValidator.Validate(new BlindDefinition
            {
                Type = BlindType.Venetian,
                ShadePosition = 120,
                Slats = new SlatDefinition { Width = 0, MinAngle = 10, MaxAngle = 10 }
            });
            Assert.Contains(problems, a => a.Path == "shadePosition");
            Assert.Contains(problems, a => a.Code == "GEOMETRY");
            Assert.Contains(problems, a => a.Code == "ANGLES");
        }
    }
}
