using System.Threading.Tasks;
using P3.Synology.Api.Client.ApiDescription;
using P3.Synology.Api.Client.Apis.Auth;
using P3.Synology.Api.Client.Apis.DownloadStation;
using P3.Synology.Api.Client.Apis.FileStation;
using P3.Synology.Api.Client.Apis.Info;
using P3.Synology.Api.Client.Session;

namespace P3.Synology.Api.Client
{
    public interface ISynologyClient
    {
        IApisInfo ApisInfo { get; set; }

        ISynologySession Session { get; set; }

        bool IsLoggedIn { get; }

        IInfoEndpoint InfoApi();

        IAuthApi AuthApi();

        IDownloadStationApi DownloadStationApi();

        IFileStationApi FileStationApi();

        Task LoginAsync(string username, string password, string otpCode = "");

        Task LogoutAsync();
    }
}
