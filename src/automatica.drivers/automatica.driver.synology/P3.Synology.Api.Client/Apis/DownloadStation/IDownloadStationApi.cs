using P3.Synology.Api.Client.Apis.DownloadStation.Task;

namespace P3.Synology.Api.Client.Apis.DownloadStation
{
    public interface IDownloadStationApi
    {
        IDownloadStationTaskEndpoint TaskEndpoint();
    }
}
