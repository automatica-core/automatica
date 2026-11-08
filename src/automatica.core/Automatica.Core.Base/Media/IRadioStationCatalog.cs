#nullable enable
using System.Collections.Generic;

namespace Automatica.Core.Base.Media
{
    /// <summary>
    /// The radio stations of the core: the built-in list plus the stations the administrator added
    /// </summary>
    public interface IRadioStationCatalog
    {
        /// <summary>
        /// All stations, sorted by country and name
        /// </summary>
        IReadOnlyList<RadioStation> Stations { get; }

        /// <summary>
        /// The station with this key (or, for older configurations, this name), case insensitive; null if there is none
        /// </summary>
        RadioStation? Find(string? keyOrName);
    }
}
