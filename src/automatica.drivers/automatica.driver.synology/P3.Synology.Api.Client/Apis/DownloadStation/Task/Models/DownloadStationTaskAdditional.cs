using System.Collections.Generic;

namespace P3.Synology.Api.Client.Apis.DownloadStation.Task.Models
{
    public class DownloadStationTaskAdditional
    {
        public DownloadStationTaskDetail Detail { get; set; }

        public IEnumerable<DownloadStationTaskFile> File { get; set; }
    }
}
