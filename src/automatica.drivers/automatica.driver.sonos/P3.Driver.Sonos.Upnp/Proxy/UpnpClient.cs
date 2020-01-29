using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp.Proxy.Soap;

namespace P3.Driver.Sonos.Upnp.Proxy
{
    public class UpnpClient : IUpnpClient
    {
        private readonly Uri _upnUri;
        private readonly IUpnpRequestContentFactory _upnpRequestContentFactory;

        private string ActionNamespace { get; }

        private static readonly HttpClient HttpClient = new HttpClient();

        public UpnpClient(Uri upnpUri, string actionNamespace)
        {
            _upnpRequestContentFactory = new UpnpRequestContentFactory();
            _upnUri = upnpUri;
            ActionNamespace = actionNamespace;
        }

        public async Task<HttpResponseMessage> InvokeActionAsync(string actionName)
        {
            return await InvokeActionAsync(actionName, null);
        }

        public async Task<HttpResponseMessage> InvokeActionAsync(string actionName, IList<UpnpArgument> upnpArgs)
        {
            var soapAction = new SoapAction(actionName, ActionNamespace);

            var content = _upnpRequestContentFactory.CreateFor(soapAction, new UpnpArgumentList(upnpArgs));

            return await HttpClient.PostAsync(_upnUri, content);
        }

        public async Task<T> InvokeFuncAsync<T>(string actionName)
        {
            return await InvokeFuncAsync<T>(actionName, null);
        }

        public async Task<T> InvokeFuncAsync<T>(string actionName, IList<UpnpArgument> upnpArgs)
        {
            string result = await InvokeFuncWithResultAsync(actionName, upnpArgs);

            var singleResult = ((XElement)((XElement)((XElement)XElement.Parse(result).FirstNode).FirstNode).FirstNode).Value;

            return (T)Convert.ChangeType(singleResult, typeof(T), CultureInfo.CurrentCulture);
        }

        public Task<string> InvokeFuncWithResultAsync(string action)
        {
            return InvokeFuncWithResultAsync(action, null);
        }

        public async Task<string> InvokeFuncWithResultAsync(string action, IList<UpnpArgument> upnpArgs)
        {
            var soapAction = new SoapAction(action, ActionNamespace);

            var content = _upnpRequestContentFactory.CreateFor(soapAction, new UpnpArgumentList(upnpArgs));
            
            var response = await HttpClient.PostAsync(_upnUri, content);

            var bytes = await response.Content.ReadAsByteArrayAsync();

            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }        
    }
}