using System.Text.Json.Serialization;

namespace Automatica.Core.Shading.Definition
{
    /// <summary>
    /// Where a value comes from. A <see cref="Signal"/> lists sources in priority order; the first one that has a fresh
    /// value is used, so a weather station can be mixed with calculated or online data (sensor first, calculation as
    /// fallback when the sensor is missing or stale).
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(DatapointSource), "datapoint")]
    [JsonDerivedType(typeof(CalculatedSource), "calculated")]
    [JsonDerivedType(typeof(IrradianceSource), "irradiance")]
    [JsonDerivedType(typeof(PvSource), "pv")]
    public abstract class SignalSource
    {
    }

    /// <summary>
    /// Brightness only: the global horizontal irradiance (W/m²) of a weather station's pyranometer. It is split into
    /// direct and diffuse light (Erbs) and projected onto the facade, so one sensor on the roof serves every facade.
    /// </summary>
    public sealed class IrradianceSource : SignalSource
    {
        public Guid Node { get; set; }
        public double Factor { get; set; } = 1;
        public double Offset { get; set; }
        public int StaleAfterMinutes { get; set; } = 15;
    }

    /// <summary>
    /// Brightness only: the power of a PV system tells how much of the clear sky sun gets through. The measured power is
    /// compared with what the panels would produce under a clear sky; the share of direct sun is then applied to the
    /// calculated brightness of the facade. Works while the sun shines onto the panels (not in the early morning or late
    /// evening for a south roof), otherwise the next source is used.
    /// </summary>
    public sealed class PvSource : SignalSource
    {
        /// <summary>
        /// AC or DC power of the system (or of one string)
        /// </summary>
        public Guid Node { get; set; }

        /// <summary>
        /// Converts the value to W (1000 for kW)
        /// </summary>
        public double Factor { get; set; } = 1;

        /// <summary>
        /// Peak power (Wp) of the panels behind the measured power
        /// </summary>
        public double PeakPower { get; set; } = 10000;

        /// <summary>
        /// Panel direction (0 north, 180 south)
        /// </summary>
        public double Azimuth { get; set; } = 180;

        /// <summary>
        /// Panel tilt (0 flat, 90 vertical)
        /// </summary>
        public double Tilt { get; set; } = 30;

        /// <summary>
        /// Losses of inverter, cables, heat and dirt (0..1)
        /// </summary>
        public double PerformanceRatio { get; set; } = 0.85;

        public int StaleAfterMinutes { get; set; } = 15;
    }

    /// <summary>
    /// The value of a datapoint (node instance or logic output), e.g. a KNX weather station or the OpenWeatherMap driver.
    /// The value is converted with <c>value * Factor + Offset</c> (e.g. 1000 to convert klx to lx, 1/3.6 for km/h to m/s).
    /// </summary>
    public sealed class DatapointSource : SignalSource
    {
        public Guid Node { get; set; }
        public double Factor { get; set; } = 1;
        public double Offset { get; set; }

        /// <summary>
        /// The value is ignored when it was not updated for that long (0: never stale)
        /// </summary>
        public int StaleAfterMinutes { get; set; }
    }

    /// <summary>
    /// Calculated from the sun position of the location in the settings. For the brightness this is a clear sky model,
    /// attenuated by <see cref="CloudCover"/> (percent, e.g. from the OpenWeatherMap driver) when it is set.
    /// For the sun position it is the astronomical calculation. Other signals cannot be calculated.
    /// </summary>
    public sealed class CalculatedSource : SignalSource
    {
        /// <summary>
        /// Optional cloud cover (0..100 %) used by the brightness model
        /// </summary>
        public Signal CloudCover { get; set; }
    }

    public sealed class Signal
    {
        public List<SignalSource> Sources { get; set; } = new();

        public static Signal Calculated() => new() { Sources = { new CalculatedSource() } };

        public static Signal FromDatapoint(Guid node, double factor = 1, int staleAfterMinutes = 0) =>
            new() { Sources = { new DatapointSource { Node = node, Factor = factor, StaleAfterMinutes = staleAfterMinutes } } };
    }

    public enum ManualOverrideMode
    {
        /// <summary>A manual move pauses the automatic until the shading demand changes (sun comes or goes)</summary>
        UntilChange,
        /// <summary>A manual move pauses the automatic for <see cref="ShadingSettingsDefinition.ManualOverrideMinutes"/></summary>
        Duration,
        /// <summary>A manual move pauses the automatic until midnight</summary>
        UntilEndOfDay
    }

    /// <summary>
    /// Brightness thresholds with hysteresis: direct sun when the brightness is at least <see cref="OnLux"/> for
    /// <see cref="OnDelayMinutes"/>, no direct sun when it is below <see cref="OffLux"/> for <see cref="OffDelayMinutes"/>.
    /// </summary>
    public sealed class SunThresholds
    {
        public double OnLux { get; set; } = 20000;
        public double OffLux { get; set; } = 12000;
        public int OnDelayMinutes { get; set; } = 2;
        public int OffDelayMinutes { get; set; } = 15;
    }

    /// <summary>
    /// Global settings of the automatic shading (one per server)
    /// </summary>
    public sealed class ShadingSettingsDefinition
    {
        public const int CurrentSchema = 1;

        public int Schema { get; set; } = CurrentSchema;

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Sun azimuth in degrees (0 north, 90 east, 180 south, 270 west)
        /// </summary>
        public Signal SunAzimuth { get; set; } = Signal.Calculated();

        /// <summary>
        /// Sun elevation in degrees above the horizon
        /// </summary>
        public Signal SunElevation { get; set; } = Signal.Calculated();

        /// <summary>
        /// Brightness in lux, used for facades without their own brightness signal (or when it has no value)
        /// </summary>
        public Signal Brightness { get; set; } = Signal.Calculated();

        public SunThresholds Thresholds { get; set; } = new();

        /// <summary>
        /// Wind speed in m/s
        /// </summary>
        public Signal Wind { get; set; } = new();

        /// <summary>
        /// Blinds with wind protection retract at this wind speed (m/s, 0: off)
        /// </summary>
        public double WindLimit { get; set; } = 10;

        /// <summary>
        /// The wind alarm ends when the wind was below the limit for that long
        /// </summary>
        public int WindReleaseMinutes { get; set; } = 10;

        /// <summary>
        /// Rain: true, or a value above 0 (e.g. mm/h) means rain
        /// </summary>
        public Signal Rain { get; set; } = new();

        public int RainReleaseMinutes { get; set; } = 10;

        /// <summary>
        /// Outside temperature in °C
        /// </summary>
        public Signal OutsideTemperature { get; set; } = new();

        /// <summary>
        /// Below this outside temperature (°C) the automatic does not move blinds, so frozen blinds are not damaged (null: off)
        /// </summary>
        public double? FrostBelow { get; set; }

        public ManualOverrideMode ManualOverride { get; set; } = ManualOverrideMode.UntilChange;
        public int ManualOverrideMinutes { get; set; } = 120;

        /// <summary>
        /// While shading, blinds are moved at most that often (sun tracking of slats, position corrections)
        /// </summary>
        public int MinMoveIntervalMinutes { get; set; } = 10;

        /// <summary>
        /// While shading, smaller changes (percent) are not sent
        /// </summary>
        public int MinPositionChange { get; set; } = 10;
        public int MinSlatChange { get; set; } = 10;

        /// <summary>
        /// Heating season: blinds that allow it let the sun in to warm the rooms
        /// </summary>
        public WinterDefinition Winter { get; set; } = new();
    }

    public enum WinterMode
    {
        /// <summary>No winter mode, shade all year</summary>
        Off,
        /// <summary>Winter while the outside temperature is below <see cref="WinterDefinition.Below"/></summary>
        Temperature,
        /// <summary>Winter from <see cref="WinterDefinition.FromMonth"/> to <see cref="WinterDefinition.ToMonth"/></summary>
        Months
    }

    /// <summary>
    /// Winter mode: no shading for the warmth of the sun (passive solar gain). Blinds opt out with
    /// <see cref="BlindDefinition.WinterGain"/> (e.g. glare in an office), and a blind with a room temperature still shades
    /// when the room is warmer than <see cref="BlindDefinition.MaxRoomTemperature"/>.
    /// </summary>
    public sealed class WinterDefinition
    {
        public WinterMode Mode { get; set; } = WinterMode.Off;

        /// <summary>
        /// Winter below this outside temperature (°C); it ends 1 °C above
        /// </summary>
        public double Below { get; set; } = 15;

        /// <summary>
        /// First and last winter month (1..12, may wrap the year: 10..4)
        /// </summary>
        public int FromMonth { get; set; } = 10;
        public int ToMonth { get; set; } = 4;
    }

    /// <summary>
    /// A facade: the direction the windows face and optionally the brightness sensor of that side of the building.
    /// </summary>
    public sealed class FacadeDefinition
    {
        public const int CurrentSchema = 1;

        public int Schema { get; set; } = CurrentSchema;

        /// <summary>
        /// Brightness in lux on this facade (e.g. "brightness east" of a weather station). Falls back to the global brightness.
        /// </summary>
        public Signal Brightness { get; set; } = new();

        /// <summary>
        /// Null: the global thresholds
        /// </summary>
        public SunThresholds Thresholds { get; set; }

        /// <summary>
        /// The sun reaches the windows when it is at most that many degrees left or right of the facade direction
        /// </summary>
        public double AcceptanceAngle { get; set; } = 80;

        /// <summary>
        /// Below this sun elevation the windows are shaded by the horizon (trees, buildings)
        /// </summary>
        public double MinElevation { get; set; } = 5;

        /// <summary>
        /// Above this sun elevation the windows are shaded by an overhang (balcony, roof)
        /// </summary>
        public double MaxElevation { get; set; } = 90;

        /// <summary>
        /// Only shade when the outside temperature is at least this value (°C, null: always, glare protection)
        /// </summary>
        public double? MinOutsideTemperature { get; set; }
    }

    public enum BlindType
    {
        /// <summary>Roller shutter / blind without slats ("Rollo")</summary>
        Roller,
        /// <summary>Venetian blind with tiltable slats ("Jalousie", "Raffstore")</summary>
        Venetian
    }

    /// <summary>
    /// Slat geometry of a venetian blind and how the slat angle maps to the actuator's percent.
    /// Angles: 0° horizontal (open), 90° closed.
    /// </summary>
    public sealed class SlatDefinition
    {
        /// <summary>
        /// Follow the sun: the smallest angle that still blocks direct sunlight. Otherwise <see cref="FixedAngle"/>.
        /// </summary>
        public bool TrackSun { get; set; } = true;
        public double FixedAngle { get; set; } = 45;

        /// <summary>
        /// Width of a slat (mm)
        /// </summary>
        public double Width { get; set; } = 80;

        /// <summary>
        /// Vertical distance between two slats (mm)
        /// </summary>
        public double Spacing { get; set; } = 72;

        /// <summary>
        /// Added to the calculated angle as safety margin (degrees)
        /// </summary>
        public double ExtraAngle { get; set; } = 5;

        public double MinAngle { get; set; } = 0;
        public double MaxAngle { get; set; } = 90;
        public double PercentAtMinAngle { get; set; } = 0;
        public double PercentAtMaxAngle { get; set; } = 100;

        /// <summary>
        /// Slat position (percent) after the shading ends
        /// </summary>
        public int OpenPercent { get; set; } = 0;
    }

    /// <summary>
    /// The shading settings of one blind (control)
    /// </summary>
    public sealed class BlindDefinition
    {
        public const int CurrentSchema = 1;

        public int Schema { get; set; } = CurrentSchema;

        public BlindType Type { get; set; } = BlindType.Roller;

        /// <summary>
        /// Own direction of the window (degrees), null: the facade's
        /// </summary>
        public double? Azimuth { get; set; }

        public double? AcceptanceAngle { get; set; }
        public double? MinElevation { get; set; }
        public double? MaxElevation { get; set; }

        /// <summary>
        /// Position while shading (percent, 100 closed)
        /// </summary>
        public int ShadePosition { get; set; } = 100;

        /// <summary>
        /// When the shading ends, the position before the shading is restored. Otherwise the blind opens to <see cref="OpenPosition"/>.
        /// </summary>
        public bool RestorePosition { get; set; } = true;
        public int OpenPosition { get; set; } = 0;

        public SlatDefinition Slats { get; set; } = new();

        /// <summary>
        /// Retracts (opens) on wind alarm. Null: venetian blinds yes, roller shutters no.
        /// </summary>
        public bool? WindProtection { get; set; }

        /// <summary>
        /// Own wind limit (m/s), null: the global one
        /// </summary>
        public double? WindLimit { get; set; }

        /// <summary>
        /// Retracts on rain (fabric awnings, some external venetian blinds)
        /// </summary>
        public bool RainProtection { get; set; }

        /// <summary>
        /// Contact of a door behind the blind (true/1 = open): the automatic never lowers the blind while the door is
        /// open, so nobody is locked out on the terrace
        /// </summary>
        public Guid? DoorContact { get; set; }

        /// <summary>
        /// Time the blind needs from open to closed (s). Position changes in this time after an automatic move are
        /// not taken as a manual move.
        /// </summary>
        public int TravelSeconds { get; set; } = 90;

        /// <summary>
        /// In winter mode the blind lets the sun in (false: it shades all year, e.g. against glare)
        /// </summary>
        public bool WinterGain { get; set; } = true;

        /// <summary>
        /// Temperature of the room behind the blind (°C)
        /// </summary>
        public Signal RoomTemperature { get; set; } = new();

        /// <summary>
        /// In winter mode the blind shades anyway when the room is warmer than this (°C, null: never)
        /// </summary>
        public double? MaxRoomTemperature { get; set; }
    }
}
