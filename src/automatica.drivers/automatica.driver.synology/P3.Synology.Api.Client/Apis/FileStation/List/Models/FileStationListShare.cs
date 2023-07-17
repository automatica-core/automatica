using System.Text.Json.Serialization;

namespace P3.Synology.Api.Client.Apis.FileStation.List.Models
{
    public class FileStationListShare
    {
        [JsonPropertyName("isdir")]
        public bool IsDir { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public FileStationListShareAdditional Additional { get; set; }
    }
}
