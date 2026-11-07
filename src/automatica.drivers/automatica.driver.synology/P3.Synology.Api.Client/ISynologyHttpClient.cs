using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using P3.Synology.Api.Client.ApiDescription;
using P3.Synology.Api.Client.Session;

namespace P3.Synology.Api.Client
{
    public interface ISynologyHttpClient
    {
        Task<T> GetAsync<T>(IApiInfo apiInfo, string apiMethod, Dictionary<string, string> queryParams, ISynologySession? session = null);

        Task<T> PostAsync<T>(IApiInfo apiInfo, string apiMethod, HttpContent content, ISynologySession? session = null);

        /// <summary>
        /// Sends api, version, method and the parameters as form body, so nothing ends up in URLs or access logs
        /// </summary>
        Task<T> PostFormAsync<T>(IApiInfo apiInfo, string apiMethod, Dictionary<string, string> formParams, ISynologySession? session = null);
    }
}
