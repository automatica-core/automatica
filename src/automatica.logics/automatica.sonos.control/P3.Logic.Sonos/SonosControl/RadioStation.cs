using System;
using System.Collections.Generic;
using Automatica.Core.Base.Media;

namespace P3.Logic.Sonos.SonosControl;

/// <summary>
/// A station of the radio station list of Sonos Control: a station key of the core's catalog (e.g. "at-oe3"), or as
/// configured before the catalog a TuneIn id (e.g. s8007) or a stream url. The Sonos driver translates the id.
/// </summary>
public sealed record RadioStation(string Name, string Id, string Logo = null)
{
    /// <summary>
    /// "at-oe3;de-1live" (station keys, also one per line) or the older "Name=station;Name=station". A key of the catalog
    /// gets the name and logo of the catalog; any other station without a name is shown with its id. A url without a name
    /// is kept whole, even when it has a "=" in its query.
    /// </summary>
    public static IReadOnlyList<RadioStation> ParseList(string text, IRadioStationCatalog catalog = null)
    {
        var ret = new List<RadioStation>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return ret;
        }

        foreach (var entry in text.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = entry.IndexOf('=');
            var isNamed = separator >= 0 && !entry[..separator].Contains("://") && !entry[..separator].Contains('?');
            var name = isNamed ? entry[..separator].Trim() : entry;
            var id = isNamed ? entry[(separator + 1)..].Trim() : entry;
            if (id.Length == 0)
            {
                continue;
            }

            var known = catalog?.Find(id);
            if (known != null && string.Equals(known.Key, id, StringComparison.OrdinalIgnoreCase))
            {
                ret.Add(new RadioStation(isNamed && name.Length > 0 ? name : known.Name, known.Key, known.Logo));
                continue;
            }

            ret.Add(new RadioStation(name.Length == 0 ? id : name, id));
        }
        return ret;
    }
}
