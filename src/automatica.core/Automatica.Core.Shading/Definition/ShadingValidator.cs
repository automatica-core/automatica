namespace Automatica.Core.Shading.Definition
{
    public sealed record ShadingProblem(string Path, string Code, string Detail = null);

    /// <summary>
    /// Checks values that would make the automatic misbehave (not the references, the API checks those)
    /// </summary>
    public static class ShadingValidator
    {
        public static IReadOnlyList<ShadingProblem> Validate(ShadingSettingsDefinition settings)
        {
            var problems = new List<ShadingProblem>();
            ValidateThresholds(settings.Thresholds, "thresholds", problems);
            ValidateSignal(settings.SunAzimuth, "sunAzimuth", problems, SignalKind.Sun);
            ValidateSignal(settings.SunElevation, "sunElevation", problems, SignalKind.Sun);
            ValidateSignal(settings.Brightness, "brightness", problems, SignalKind.Brightness);
            ValidateSignal(settings.Wind, "wind", problems, SignalKind.Measured);
            ValidateSignal(settings.Rain, "rain", problems, SignalKind.Measured);
            ValidateSignal(settings.OutsideTemperature, "outsideTemperature", problems, SignalKind.Measured);

            var winter = settings.Winter ?? new WinterDefinition();
            if (winter.FromMonth is < 1 or > 12 || winter.ToMonth is < 1 or > 12)
            {
                problems.Add(new ShadingProblem("winter", "OUT_OF_RANGE", "1..12"));
            }
            if (winter.Mode == WinterMode.Temperature && settings.OutsideTemperature.Sources.Count == 0)
            {
                problems.Add(new ShadingProblem("winter", "TEMPERATURE_REQUIRED", "winter by temperature needs an outside temperature"));
            }

            if (settings.WindLimit < 0)
            {
                problems.Add(new ShadingProblem("windLimit", "NEGATIVE"));
            }
            if (settings.WindReleaseMinutes < 0 || settings.RainReleaseMinutes < 0 || settings.ManualOverrideMinutes < 0 || settings.MinMoveIntervalMinutes < 0)
            {
                problems.Add(new ShadingProblem("minutes", "NEGATIVE"));
            }
            if (settings.MinPositionChange is < 0 or > 100 || settings.MinSlatChange is < 0 or > 100)
            {
                problems.Add(new ShadingProblem("minChange", "OUT_OF_RANGE", "0..100"));
            }

            return problems;
        }

        public static IReadOnlyList<ShadingProblem> Validate(string name, double azimuth, FacadeDefinition facade)
        {
            var problems = new List<ShadingProblem>();
            if (string.IsNullOrWhiteSpace(name))
            {
                problems.Add(new ShadingProblem("name", "REQUIRED"));
            }
            if (azimuth is < 0 or >= 360 || double.IsNaN(azimuth))
            {
                problems.Add(new ShadingProblem("azimuth", "OUT_OF_RANGE", "0..359.9"));
            }
            if (facade.Thresholds != null)
            {
                ValidateThresholds(facade.Thresholds, "thresholds", problems);
            }
            ValidateSignal(facade.Brightness, "brightness", problems, SignalKind.Brightness);
            ValidateAngles(facade.AcceptanceAngle, facade.MinElevation, facade.MaxElevation, problems);
            return problems;
        }

        public static IReadOnlyList<ShadingProblem> Validate(BlindDefinition blind)
        {
            var problems = new List<ShadingProblem>();
            if (blind.Azimuth is < 0 or >= 360)
            {
                problems.Add(new ShadingProblem("azimuth", "OUT_OF_RANGE", "0..359.9"));
            }
            ValidateAngles(blind.AcceptanceAngle, blind.MinElevation, blind.MaxElevation, problems);

            if (blind.ShadePosition is < 0 or > 100)
            {
                problems.Add(new ShadingProblem("shadePosition", "OUT_OF_RANGE", "0..100"));
            }
            if (blind.OpenPosition is < 0 or > 100)
            {
                problems.Add(new ShadingProblem("openPosition", "OUT_OF_RANGE", "0..100"));
            }
            if (blind.WindLimit is < 0)
            {
                problems.Add(new ShadingProblem("windLimit", "NEGATIVE"));
            }
            ValidateSignal(blind.RoomTemperature, "roomTemperature", problems, SignalKind.Measured);
            if (blind.TravelSeconds is < 0 or > 600)
            {
                problems.Add(new ShadingProblem("travelSeconds", "OUT_OF_RANGE", "0..600"));
            }

            if (blind.Type == BlindType.Venetian)
            {
                var slats = blind.Slats;
                if (slats.Width <= 0 || slats.Spacing <= 0)
                {
                    problems.Add(new ShadingProblem("slats", "GEOMETRY", "width and spacing must be above 0"));
                }
                if (slats.MinAngle is < 0 or > 180 || slats.MaxAngle is < 0 or > 180 || Math.Abs(slats.MaxAngle - slats.MinAngle) < 1)
                {
                    problems.Add(new ShadingProblem("slats", "ANGLES", "0..180, min and max must differ"));
                }
                if (slats.PercentAtMinAngle is < 0 or > 100 || slats.PercentAtMaxAngle is < 0 or > 100 || slats.OpenPercent is < 0 or > 100)
                {
                    problems.Add(new ShadingProblem("slats", "OUT_OF_RANGE", "0..100"));
                }
            }

            return problems;
        }

        private static void ValidateAngles(double? acceptance, double? minElevation, double? maxElevation, List<ShadingProblem> problems)
        {
            if (acceptance is <= 0 or > 90)
            {
                problems.Add(new ShadingProblem("acceptanceAngle", "OUT_OF_RANGE", "1..90"));
            }
            if (minElevation is < 0 or > 90 || maxElevation is < 0 or > 90)
            {
                problems.Add(new ShadingProblem("elevation", "OUT_OF_RANGE", "0..90"));
            }
            if (minElevation.HasValue && maxElevation.HasValue && minElevation.Value >= maxElevation.Value)
            {
                problems.Add(new ShadingProblem("elevation", "MIN_ABOVE_MAX"));
            }
        }

        private static void ValidateThresholds(SunThresholds thresholds, string path, List<ShadingProblem> problems)
        {
            if (thresholds.OffLux < 0 || thresholds.OnLux <= 0)
            {
                problems.Add(new ShadingProblem(path, "OUT_OF_RANGE"));
            }
            if (thresholds.OffLux > thresholds.OnLux)
            {
                problems.Add(new ShadingProblem(path, "OFF_ABOVE_ON", "the off threshold must not be above the on threshold"));
            }
            if (thresholds.OnDelayMinutes < 0 || thresholds.OffDelayMinutes < 0)
            {
                problems.Add(new ShadingProblem(path, "NEGATIVE"));
            }
        }

        private enum SignalKind
        {
            /// <summary>Sun position: datapoint or astronomical calculation</summary>
            Sun,
            /// <summary>Brightness: datapoint, calculation, irradiance or PV</summary>
            Brightness,
            /// <summary>Wind, rain, temperatures, cloud cover: datapoints only</summary>
            Measured
        }

        private static void ValidateSignal(Signal signal, string path, List<ShadingProblem> problems, SignalKind kind)
        {
            for (var i = 0; i < signal.Sources.Count; i++)
            {
                var at = $"{path}/{i}";
                switch (signal.Sources[i])
                {
                    case DatapointSource { Node: var node } when node == Guid.Empty:
                        problems.Add(new ShadingProblem(at, "DATAPOINT_REQUIRED"));
                        break;
                    case DatapointSource { StaleAfterMinutes: < 0 }:
                        problems.Add(new ShadingProblem(at, "NEGATIVE"));
                        break;
                    case CalculatedSource when kind == SignalKind.Measured:
                        problems.Add(new ShadingProblem(at, "NOT_CALCULABLE", "only the sun position and the brightness can be calculated"));
                        break;
                    case CalculatedSource { CloudCover: { } cloud }:
                        ValidateSignal(cloud, $"{at}/cloudCover", problems, SignalKind.Measured);
                        break;
                    case IrradianceSource or PvSource when kind != SignalKind.Brightness:
                        problems.Add(new ShadingProblem(at, "BRIGHTNESS_ONLY", "irradiance and PV sources only give a brightness"));
                        break;
                    case IrradianceSource irradiance:
                        if (irradiance.Node == Guid.Empty)
                        {
                            problems.Add(new ShadingProblem(at, "DATAPOINT_REQUIRED"));
                        }
                        if (irradiance.StaleAfterMinutes < 0)
                        {
                            problems.Add(new ShadingProblem(at, "NEGATIVE"));
                        }
                        break;
                    case PvSource pv:
                        if (pv.Node == Guid.Empty)
                        {
                            problems.Add(new ShadingProblem(at, "DATAPOINT_REQUIRED"));
                        }
                        if (pv.PeakPower <= 0 || pv.Factor <= 0)
                        {
                            problems.Add(new ShadingProblem(at, "OUT_OF_RANGE", "peak power and factor must be above 0"));
                        }
                        if (pv.Azimuth is < 0 or >= 360 || pv.Tilt is < 0 or > 90)
                        {
                            problems.Add(new ShadingProblem(at, "OUT_OF_RANGE", "azimuth 0..359.9, tilt 0..90"));
                        }
                        if (pv.PerformanceRatio is <= 0 or > 1)
                        {
                            problems.Add(new ShadingProblem(at, "OUT_OF_RANGE", "performance ratio 0..1"));
                        }
                        if (pv.StaleAfterMinutes < 0)
                        {
                            problems.Add(new ShadingProblem(at, "NEGATIVE"));
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Datapoints the document refers to
        /// </summary>
        public static IEnumerable<Guid> Datapoints(Signal signal)
        {
            foreach (var source in signal?.Sources ?? new List<SignalSource>())
            {
                switch (source)
                {
                    case DatapointSource datapoint:
                        yield return datapoint.Node;
                        break;
                    case IrradianceSource irradiance:
                        yield return irradiance.Node;
                        break;
                    case PvSource pv:
                        yield return pv.Node;
                        break;
                    case CalculatedSource { CloudCover: { } cloud }:
                        foreach (var id in Datapoints(cloud))
                        {
                            yield return id;
                        }
                        break;
                }
            }
        }
    }
}
