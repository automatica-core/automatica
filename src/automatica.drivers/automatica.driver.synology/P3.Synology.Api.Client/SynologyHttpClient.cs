using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;
using P3.Synology.Api.Client.ApiDescription;
using P3.Synology.Api.Client.Constants;
using P3.Synology.Api.Client.Errors;
using P3.Synology.Api.Client.Exceptions;
using P3.Synology.Api.Client.Session;
using P3.Synology.Api.Client.Shared.Models;

namespace P3.Synology.Api.Client
{
    public class SynologyHttpClient : ISynologyHttpClient
    {
        private readonly HttpClient _httpClient;

        public SynologyHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T> GetAsync<T>(IApiInfo apiInfo, string apiMethod, Dictionary<string, string> queryParams, ISynologySession? session = null)
        {
            var uri = GetBaseUri(_httpClient.BaseAddress, apiInfo.Path);
            var uriBuilder = new UriBuilder(uri);

            uriBuilder.Query = BuildQueryString(uriBuilder, apiInfo, apiMethod, queryParams, session);

            using var response = await _httpClient.GetAsync(uriBuilder.Uri);
            return await HandleSynologyResponse<T>(response, apiInfo, apiMethod);
        }

        public async Task<T> PostAsync<T>(IApiInfo apiInfo, string apiMethod, HttpContent content, ISynologySession? session = null)
        {
            var uri = GetBaseUri(_httpClient.BaseAddress, apiInfo.Path);
            var uriBuilder = new UriBuilder(uri);

            if (session != null)
            {
                uriBuilder.Query = $"_sid={session.Sid}";
            }

            //just for debugging to check values more easily
            //var headersAsString = content.Headers.ToString();
            //var contentAsString = await content.ReadAsStringAsync();

            using var response = await _httpClient.PostAsync(uriBuilder.Uri, content);
            return await HandleSynologyResponse<T>(response, apiInfo, apiMethod);
        }

        public async Task<T> PostFormAsync<T>(IApiInfo apiInfo, string apiMethod, Dictionary<string, string> formParams, ISynologySession? session = null)
        {
            var uri = GetBaseUri(_httpClient.BaseAddress, apiInfo.Path);

            var form = new Dictionary<string, string>
            {
                ["api"] = apiInfo.Name,
                ["version"] = apiInfo.Version.ToString(),
                ["method"] = apiMethod
            };

            foreach (var curPair in formParams.Where(a => a.Value != null))
            {
                form[curPair.Key] = curPair.Value;
            }

            if (!string.IsNullOrWhiteSpace(apiInfo.SessionName))
            {
                form["session"] = apiInfo.SessionName;
            }

            if (session != null)
            {
                form["_sid"] = session.Sid;
            }

            using var content = new FormUrlEncodedContent(form);
            using var response = await _httpClient.PostAsync(uri, content);
            return await HandleSynologyResponse<T>(response, apiInfo, apiMethod);
        }

        private static Uri GetBaseUri(Uri? baseAddress, string apiPath)
        {
            if (baseAddress == null)
            {
                throw new InvalidOperationException("The HttpClient has no BaseAddress, the DSM url must be set.");
            }

            var baseUri = baseAddress.ToString().TrimEnd('/');
            apiPath = apiPath.TrimStart('/');

            return new Uri(baseUri + "/" + apiPath);
        }

        private string BuildQueryString(UriBuilder uriBuilder, IApiInfo apiInfo, string apiMethod, Dictionary<string, string> queryParams, ISynologySession? session = null)
        {
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["api"] = apiInfo.Name;
            query["version"] = apiInfo.Version.ToString();
            query["method"] = apiMethod;

            foreach (var curPair in queryParams)
            {
                query[curPair.Key] = curPair.Value;
            }

            if (!string.IsNullOrWhiteSpace(apiInfo.SessionName))
            {
                query["session"] = apiInfo.SessionName;
            }

            if (session != null)
            {
                query["_sid"] = session.Sid;
            }

            return query.ToString() ?? string.Empty;
        }

        private async Task<T> HandleSynologyResponse<T>(HttpResponseMessage httpResponse, IApiInfo apiInfo, string apiMethod)
        {
            switch (httpResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    {
                        var response = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<T>>()
                            ?? throw new UnexpectedResponseStatusException("Received an empty response body.", httpResponse.StatusCode);
                        if (!response.Success)
                        {
                            // a failed call does not always carry an error object
                            var errorCode = response.Error?.Code ?? 0;
                            var errorDescription = GetErrorMessage(errorCode, apiInfo.Name);

                            var synologyApiException = new SynologyApiException(apiInfo, apiMethod, errorCode, errorDescription);
                            //add additional error details if present
                            var errors = response.Error?.Errors;
                            if (errors != null)
                            {
                                foreach (var curError in errors)
                                {
                                    var errorMessage = GetErrorMessage(curError.Code, apiInfo.Name);
                                    synologyApiException.Data.Add($"[{curError.Code}] {errorMessage}", curError.Path);
                                }
                            }

                            throw synologyApiException;
                        }

                        if (typeof(T) == typeof(BaseApiResponse))
                        {
                            return (T)Activator.CreateInstance(typeof(T), new object[] { response.Success })!;
                        }

                        // Synology omits data for calls that only report success
                        return response.Data!;
                    }
                default:
                    throw new UnexpectedResponseStatusException(httpResponse.StatusCode);
            }
        }

        private string GetErrorMessage(int errorCode, string apiName)
        {
            string? errorDescription = null;

            if (ErrorMessages.CommonErrors.ContainsKey(errorCode))
            {
                ErrorMessages.CommonErrors.TryGetValue(errorCode, out errorDescription);
            }
            else if (apiName == ApiNames.AuthApiName)
            {
                ErrorMessages.AuthApiErrors.TryGetValue(errorCode, out errorDescription);
            }
            else if (apiName.Contains("FileStation"))
            {
                ErrorMessages.FileStationApiErrors.TryGetValue(errorCode, out errorDescription);
            }
            else if (apiName == ApiNames.DownloadStationTaskApiName)
            {
                ErrorMessages.DownloadStationTaskApiErrors.TryGetValue(errorCode, out errorDescription);
            }
            else if (apiName == ApiNames.DownloadStationBtSearchApiName)
            {
                ErrorMessages.DownloadStationBtSearchApiErrors.TryGetValue(errorCode, out errorDescription);
            }

            return errorDescription ?? "";
        }
    }
}
