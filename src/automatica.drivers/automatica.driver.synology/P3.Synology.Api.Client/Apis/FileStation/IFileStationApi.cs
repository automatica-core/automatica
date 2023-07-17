using P3.Synology.Api.Client.Apis.FileStation.CopyMove;
using P3.Synology.Api.Client.Apis.FileStation.CreateFolder;
using P3.Synology.Api.Client.Apis.FileStation.Extract;
using P3.Synology.Api.Client.Apis.FileStation.List;
using P3.Synology.Api.Client.Apis.FileStation.Search;
using P3.Synology.Api.Client.Apis.FileStation.Upload;

namespace P3.Synology.Api.Client.Apis.FileStation
{
    public interface IFileStationApi
    {
        IFileStationCopyMoveEndpoint CopyMoveEndpoint();

        IFileStationCreateFolderEndpoint CreateFolderEndpoint();

        IFileStationListEndpoint ListEndpoint();

        IFileStationUploadEndpoint UploadEndpoint();

        IFileStationExtractEndpoint ExtractEndpoint();
        
        IFileStationSearchEndpoint SearchEndpoint();
    }
}
