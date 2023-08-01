using System.Collections.Generic;

namespace P3.Synology.Api.Client.Apis.DownloadStation.Task.Models
{
    public class DownloadStationTaskListResponse
    {
        public int Total { get; set; }

        public int Offset { get; set; }

        public IEnumerable<DownloadStationTask> Tasks { get; set; }
    }
}
