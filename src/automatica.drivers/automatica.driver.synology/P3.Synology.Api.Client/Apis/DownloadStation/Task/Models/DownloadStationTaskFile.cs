using System.Text.Json.Serialization;

namespace P3.Synology.Api.Client.Apis.DownloadStation.Task.Models
{
    public class DownloadStationTaskFile
    {
        public string Filename { get; set; }

        public long Index { get; set; }

        public string Priority { get; set; }

        public long Size { get; set; }

        [JsonPropertyName("size_downloaded")]
        public long SizeDownloaded { get; set; }

        public bool Wanted { get; set; }
    }
}
