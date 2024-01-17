using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Common;
using Automatica.Driver.Shelly.Gen1.Options;
using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen1.Clients
{
    public abstract class ShellyClientBase : IShellyCommonClient
    {
        protected ITelegramMonitorInstance TelegramMonitor { get; }
        protected readonly HttpClient ShellyHttpClient;
        protected IShellyCommonOptions ShellyCommonOptions { get; }
        protected readonly Uri ServerUri;

        protected TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        protected ShellyClientBase(ITelegramMonitorInstance telegramMonitor, IShellyCommonOptions shellyCommonOptions)
        {
            TelegramMonitor = telegramMonitor;
            ShellyCommonOptions = shellyCommonOptions ?? throw new ArgumentNullException(nameof(shellyCommonOptions));
            ShellyHttpClient = new HttpClient { BaseAddress = new Uri($"http://{shellyCommonOptions.IpAddress}") };

            ServerUri = shellyCommonOptions.ServerUri;

            if (shellyCommonOptions.DefaultTimeout.HasValue)
            {
                DefaultTimeout = shellyCommonOptions.DefaultTimeout.Value;
            }
        }

       
        protected async Task<ShellyResult<T>> ExecuteRequestAsync<T>(HttpRequestMessage httpRequestMessage,
            CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            try
            {
                timeout = timeout ?? DefaultTimeout;

                using var timeoutTokenSource = new CancellationTokenSource(timeout.Value);
                var authenticationString = $"{ShellyCommonOptions.UserName}:{ShellyCommonOptions.Password}";
                var base64EncodedAuthenticationString =
                    Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));

                httpRequestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

                var linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);
                var response = await ShellyHttpClient.SendAsync(httpRequestMessage, linkedTokenSource.Token);

                await TelegramMonitor.NotifyTelegram(TelegramDirection.Output, "self",
                    ShellyHttpClient!.BaseAddress!.AbsoluteUri, "read", httpRequestMessage.RequestUri?.AbsoluteUri);

                if (response.StatusCode == 0)
                {
                    // Status code of 0 means timeout reached
                    return ShellyResult<T>.TransientFailure("Device did not respond within timeout period");
                }

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    return ShellyResult<T>.TransientFailure("Device responded with ServiceUnavailable");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return ShellyResult<T>.Success(default, "Device responded with NotFound");
                }

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return ShellyResult<T>.Failure($"Device responded with http status code {response.StatusCode}");
                }

                return await HandleOkResponse<T>(response);
            }
            catch (Exception ex)
            {
                return ShellyResult<T>.Failure(ex.Message);
            }
        }

        protected async Task<T> ExecuteRequestSetAsync<T>(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            timeout = timeout ?? DefaultTimeout;

            using var timeoutTokenSource = new CancellationTokenSource(timeout.Value);
            var authenticationString = $"{ShellyCommonOptions.UserName}:{ShellyCommonOptions.Password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);
            var response = await ShellyHttpClient.SendAsync(httpRequestMessage, linkedTokenSource.Token);


            var readAsStringAsync = await response.Content.ReadAsStringAsync(linkedTokenSource.Token);
            var result = JsonConvert.DeserializeObject<T>(readAsStringAsync);

            await TelegramMonitor.NotifyTelegram(TelegramDirection.Input, ShellyHttpClient!.BaseAddress!.AbsoluteUri, "self", readAsStringAsync, "");

            return result;
        }

        protected virtual async Task<ShellyResult<T>> HandleOkResponse<T>(HttpResponseMessage response)
        {
            var readAsStringAsync = await response.Content.ReadAsStringAsync();

            await TelegramMonitor.NotifyTelegram(TelegramDirection.Input, ShellyHttpClient!.BaseAddress!.AbsoluteUri, "self", readAsStringAsync, "");
            var shelly1Status = JsonConvert.DeserializeObject<T>(readAsStringAsync);
            return ShellyResult<T>.Success(shelly1Status);
        }

        public async Task<ShellyInfoDto> GetInfo(CancellationToken token = default)
        {
            var endpoint = ServerUri + "/shelly";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
            var response =  await ExecuteRequestAsync<ShellyInfoDto>(requestMessage, token, default);

            if (response.IsSuccess)
            {
                return response.Value;
            }

            return null;
        }
    }
}