using System.Threading.Tasks;
using P3.Synology.Api.Client.Apis.FileStation.Extract.Models;
using P3.Synology.Api.Client.Shared.Models;

namespace P3.Synology.Api.Client.Apis.FileStation.Extract
{
    public interface IFileStationExtractEndpoint
    {
        Task<FileStationExtractStartResponse> StartAsync(string filePath, string destination, bool overwrite);

        Task<FileStationExtractStatusResponse> GetStatusAsync(string taskId);

        Task<BaseApiResponse> StopAsync(string taskId);

        Task<FileStationExtractListResponse> ListFilesAsync(string filePath);
    }
}
