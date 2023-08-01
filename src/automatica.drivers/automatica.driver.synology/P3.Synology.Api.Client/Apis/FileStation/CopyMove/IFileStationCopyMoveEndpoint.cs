using System.Threading.Tasks;
using P3.Synology.Api.Client.Apis.FileStation.CopyMove.Models;
using P3.Synology.Api.Client.Shared.Models;

namespace P3.Synology.Api.Client.Apis.FileStation.CopyMove
{
    public interface IFileStationCopyMoveEndpoint
    {
        Task<FileStationCopyMoveStartResponse> StartCopyAsync(string[] paths, string destination, bool overwrite);

        Task<FileStationCopyMoveStartResponse> StartMoveAsync(string[] paths, string destination, bool overwrite);

        Task<FileStationCopyMoveStatusResponse> GetStatusAsync(string taskId);

        Task<BaseApiResponse> StopAsync(string taskId);
    }
}
