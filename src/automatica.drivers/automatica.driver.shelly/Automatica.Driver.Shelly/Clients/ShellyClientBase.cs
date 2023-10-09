using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Driver.Shelly.Dtos.Shelly1PM;
using Automatica.Driver.Shelly.Options;
using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Clients
{
    public abstract class ShellyClientBase
    {
        protected readonly HttpClient ShellyHttpClient;
        private readonly IShellyCommonOptions _shellyCommonOptions;
        protected readonly Uri ServerUri;
        
        protected TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        protected ShellyClientBase(HttpClient httpClient, IShellyCommonOptions shellyCommonOptions)
        {
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            if (shellyCommonOptions == null) throw new ArgumentNullException(nameof(shellyCommonOptions));
            

            ShellyHttpClient = httpClient;
            _shellyCommonOptions = shellyCommonOptions;
            ServerUri = shellyCommonOptions.ServerUri;
            
            if (shellyCommonOptions.DefaultTimeout.HasValue)
            {
                DefaultTimeout = shellyCommonOptions.DefaultTimeout.Value;
            }
        }

        public async Task<RelayDto> SetStatus(int channelId, bool value, CancellationToken token)
        {
            var turnValue = value ? "on" : "off";
            var endpoint = ServerUri + $"/relay/{channelId}?turn={turnValue}";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await ExecuteRequestSetAsync<RelayDto>(requestMessage, token, default);
        }
        protected async Task<ShellyResult<T>> ExecuteRequestAsync<T>(HttpRequestMessage httpRequestMessage,
            CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            timeout = timeout ?? DefaultTimeout;

            using var timeoutTokenSource = new CancellationTokenSource(timeout.Value);
            var authenticationString = $"{_shellyCommonOptions.UserName}:{_shellyCommonOptions.Password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));
                
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
                
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token , cancellationToken);
            var response = await ShellyHttpClient.SendAsync(httpRequestMessage, linkedTokenSource.Token);
                
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

        protected async Task<T> ExecuteRequestSetAsync<T>(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            timeout = timeout ?? DefaultTimeout;

            using var timeoutTokenSource = new CancellationTokenSource(timeout.Value);
            var authenticationString = $"{_shellyCommonOptions.UserName}:{_shellyCommonOptions.Password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);
            var response = await ShellyHttpClient.SendAsync(httpRequestMessage, linkedTokenSource.Token);


            var readAsStringAsync = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(readAsStringAsync);
            return result;
        }

        protected virtual async Task<ShellyResult<T>> HandleOkResponse<T>(HttpResponseMessage response)
        {
            var readAsStringAsync = await response.Content.ReadAsStringAsync();
            var shelly1Status = JsonConvert.DeserializeObject<T>(readAsStringAsync);
            return ShellyResult<T>.Success(shelly1Status);
        }
    }
}