#nullable enable
namespace Automatica.Core.Base.Media
{
    /// <summary>
    /// A radio station of the core's station catalog. The stream url is the way to play it; a device driver translates the
    /// station into what its device plays (e.g. Sonos plays a TuneIn id when there is one, otherwise the stream).
    /// </summary>
    public sealed record RadioStation
    {
        /// <summary>
        /// Stable key, e.g. "at-oe3": stored in logic parameters and sent to the device drivers
        /// </summary>
        public string Key { get; init; } = "";

        public string Name { get; init; } = "";

        /// <summary>
        /// ISO 3166 country code, e.g. "AT"
        /// </summary>
        public string? Country { get; init; }

        public string? Genre { get; init; }

        /// <summary>
        /// The http(s) url of the stream
        /// </summary>
        public string StreamUrl { get; init; } = "";

        /// <summary>
        /// https url of the station logo
        /// </summary>
        public string? Logo { get; init; }

        /// <summary>
        /// mp3, aac,...
        /// </summary>
        public string? Codec { get; init; }

        /// <summary>
        /// Optional TuneIn station id (e.g. "s8007"): devices that know TuneIn show its name and logo
        /// </summary>
        public string? TuneInId { get; init; }

        /// <summary>
        /// Added by the administrator (not part of the built-in list)
        /// </summary>
        public bool IsCustom { get; init; }
    }
}
