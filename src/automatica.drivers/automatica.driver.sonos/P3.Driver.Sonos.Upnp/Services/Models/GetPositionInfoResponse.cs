using System;

namespace P3.Driver.Sonos.Upnp.Services.Models
{
    public class GetPositionInfoResponse
    {
        public int TrackNumber { get; set; }

        public TimeSpan TrackDuration { get; set; }

        public string TrackMetaDataRaw { get; set; }

        public TrackMetaData TrackMetaData { get; set; }

        public string TrackUri { get; set; }

        public TimeSpan RelativeTime { get; set; }
        
        public string AbsoluteTime { get; set; }

        public int RelativeCount { get; set; }

        public int AbsoluteCount { get; set; }
    }
}