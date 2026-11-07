using System;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using P3.Synology.Api.Client.ApiDescription;
using P3.Synology.Api.Client.Apis.Auth;
using P3.Synology.Api.Client.Apis.DownloadStation;
using P3.Synology.Api.Client.Apis.FileStation;
using P3.Synology.Api.Client.Apis.Info;
using P3.Synology.Api.Client.Constants;
using P3.Synology.Api.Client.Session;

namespace P3.Synology.Api.Client
{
    public class SynologyClient : ISynologyClient
    {
        private readonly ISynologyHttpClient _synologyHttpClient;

        public SynologyClient(string dsmUrl)
            : this(dsmUrl, new HttpClient())
        {
        }

        public SynologyClient(string dsmUrl, HttpClient httpClient)
        {
            if (string.IsNullOrWhiteSpace(dsmUrl))
            {
                throw new ArgumentNullException(nameof(dsmUrl));
            }

            httpClient.BaseAddress = new Uri($"{dsmUrl.TrimEnd('/')}/webapi");

            _synologyHttpClient = new SynologyHttpClient(httpClient);

            Task.Run(() => UpdateApisInfoAsync()).Wait();
        }

        public IApisInfo ApisInfo { get; set; } = new DefaultApisInfo();

        public ISynologySession? Session { get; set; }

        public bool IsLoggedIn => Session != null && !string.IsNullOrWhiteSpace(Session.Sid);

        public IInfoEndpoint InfoApi()
        {
            return new InfoEndpoint(_synologyHttpClient, ApisInfo.InfoApi);
        }

        public IAuthApi AuthApi()
        {
            return new AuthApi(_synologyHttpClient, ApisInfo.AuthApi);
        }

        public IDownloadStationApi DownloadStationApi()
        {
            return new DownloadStationApi(_synologyHttpClient, ApisInfo, AuthenticatedSession());
        }

        public IFileStationApi FileStationApi()
        {
            return new FileStationApi(_synologyHttpClient, ApisInfo, AuthenticatedSession());
        }

        private ISynologySession AuthenticatedSession()
        {
            var session = Session;
            if (session == null || string.IsNullOrWhiteSpace(session.Sid))
            {
                throw new SecurityException(CustomErrorMessages.SessionNotAuthenticated);
            }

            return session;
        }

        public async Task LoginAsync(
            string username,
            string password,
            string otpCode = "")
        {
            var loginResult = await AuthApi().LoginAsync(username, password, otpCode);

            var sid = loginResult.Sid;
            if (sid == null || string.IsNullOrWhiteSpace(sid))
            {
                throw new SecurityException(CustomErrorMessages.SessionNotAuthenticated);
            }

            Session = new SynologySession(sid);
        }

        public async Task LogoutAsync()
        {
            var session = Session;
            if (session == null || string.IsNullOrWhiteSpace(session.Sid))
            {
                return;
            }

            await AuthApi().LogoutAsync(session.Sid);

            Session = null;
        }

        /// <summary>
        /// Updates the API descriptions using the response from the InfoApi endpoint.
        /// </summary>
        private async Task UpdateApisInfoAsync()
        {
            var infoQueryResponse = await InfoApi().QueryAsync();

            ApisInfo = DefaultApisInfo.FromInfoQueryResponse(ApisInfo, infoQueryResponse);
        }
    }
}
