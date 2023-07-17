using System.Threading.Tasks;
using P3.Synology.Api.Client.Apis.FileStation.CreateFolder.Models;

namespace P3.Synology.Api.Client.Apis.FileStation.CreateFolder
{
    public interface IFileStationCreateFolderEndpoint
    {
        Task<FileStationCreateFolderCreateResponse> CreateAsync(string[] paths, bool createParentFolders);
    }
}
