using System;

namespace P3.Driver.Sonos.Upnp.Services.Models
{
    public class QueueItemId
    {
        private const int FirstInstanceNumber = 0;
        private const int FirstTrackNumber = 1;

        public string ObjectId { get; }
        public int InstanceNumber { get; }
        public int TrackNumber { get; }

        public QueueItemId(int instanceNumber, int trackNumber)
        {
            if(instanceNumber < FirstInstanceNumber)
                throw new ArgumentOutOfRangeException(nameof(trackNumber), "Instance number cannot be less than zero.");

            if (trackNumber < FirstTrackNumber)
                throw new ArgumentOutOfRangeException(nameof(trackNumber), $"Track number cannot be less than {FirstTrackNumber}.");

            InstanceNumber = 0;
            TrackNumber = trackNumber;
            ObjectId = $"Q:{InstanceNumber}/{TrackNumber}";
        }

        public QueueItemId(int trackNumber) : this(FirstInstanceNumber, trackNumber)
        {
        }

        public QueueItemId(string objectId)
        {
            if(string.IsNullOrEmpty(objectId))
                throw new ArgumentException("ObjectId was null or empty.", nameof(objectId));

            var noPrefix = objectId.Replace("Q:", string.Empty);
            var posSlash = noPrefix.IndexOf("/", StringComparison.Ordinal);

            InstanceNumber = int.Parse(noPrefix.Substring(0, posSlash));
            TrackNumber = int.Parse(noPrefix.Substring(posSlash + 1));
            ObjectId = objectId;
        }

        public override string ToString()
        {
            return ObjectId;
        }
    }
}
