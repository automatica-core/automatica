using System.Text.Json.Serialization;

namespace P3.Synology.Api.Client.Apis.DownloadStation.Task.Models
{
    public class DownloadStationTaskStatusExtra
    {
        [JsonPropertyName("error_detail")]
        public string ErrorDetail { get; set; }
    }
}
