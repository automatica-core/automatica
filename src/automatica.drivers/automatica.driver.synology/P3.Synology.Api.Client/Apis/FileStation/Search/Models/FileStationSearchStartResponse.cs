using System.Text.Json.Serialization;

namespace P3.Synology.Api.Client.Apis.FileStation.Search.Models
{
    public class FileStationSearchStartResponse
    {
        [JsonPropertyName("taskid")]
        public string TaskId { get; set; }
    }
}