using System.Collections.Generic;
using P3.Synology.Api.Client.Apis.FileStation.List.Models;

namespace P3.Synology.Api.Client.Apis.FileStation.Search.Models
{
    public class FileStationSearchListResponse
    {
        public int Total { get; set; }

        public int Offset { get; set; }
        
        public bool Finished { get; set; }

        public IEnumerable<FileStationListShare> Files { get; set; }
    }
}