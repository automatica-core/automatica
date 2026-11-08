using System;
using System.Collections.Generic;

namespace P3.Logic.Sonos.SonosControl;

/// <summary>
/// A station of the radio station list of Sonos Control: a TuneIn id (e.g. s8007) or a stream url
/// </summary>
public sealed record RadioStation(string Name, string Id)
{
    /// <summary>
    /// "Name=station;Name=station" (also one per line); a station without a name is shown with its id.
    /// A url without a name is kept whole, even when it has a "=" in its query.
    /// </summary>
    public static IReadOnlyList<RadioStation> ParseList(string text)
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
            ret.Add(new RadioStation(name.Length == 0 ? id : name, id));
        }
        return ret;
    }
}
