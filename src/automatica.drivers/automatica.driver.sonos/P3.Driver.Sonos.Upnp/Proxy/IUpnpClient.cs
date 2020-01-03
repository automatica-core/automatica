using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace P3.Driver.Sonos.Upnp.Proxy
{
    public interface IUpnpClient
    {
        Task<HttpResponseMessage> InvokeActionAsync(string actionName);
        Task<HttpResponseMessage> InvokeActionAsync(string actionName, IList<UpnpArgument> upnpArgs);

        Task<T> InvokeFuncAsync<T>(string actionName);
        Task<T> InvokeFuncAsync<T>(string actionName, IList<UpnpArgument> upnpArgs);

        Task<string> InvokeFuncWithResultAsync(string actionName);
        Task<string> InvokeFuncWithResultAsync(string actionName, IList<UpnpArgument> upnpArgs);
    }
}