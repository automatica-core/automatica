using System.Collections.Generic;
using P3.Synology.Api.Client.Apis.FileStation.List.Models;

namespace P3.Synology.Api.Client.Apis.FileStation.CreateFolder.Models
{
    public class FileStationCreateFolderCreateResponse
    {
        public IEnumerable<FileStationListShare> Folders { get; set; }
    }
}
