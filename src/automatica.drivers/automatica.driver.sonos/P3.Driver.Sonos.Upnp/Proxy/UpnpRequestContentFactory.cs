using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using P3.Driver.Sonos.Upnp.Proxy.Soap;

namespace P3.Driver.Sonos.Upnp.Proxy
{
    internal class UpnpRequestContentFactory : IUpnpRequestContentFactory
    {
        public StringContent CreateFor(SoapAction soapAction, UpnpArgumentList args)
        {
            if(soapAction == null)
                throw new ArgumentNullException(nameof(soapAction));

            if(args == null)
                throw new ArgumentNullException(nameof(args));

            string content = CreateSoapMessage(soapAction, args);

            return CreateSoapStringContent(soapAction, content);
        }

        private static string CreateSoapMessage(SoapAction sa, UpnpArgumentList list)
        {
            var body = CreateSoapMessageBody(sa, list);

            return $"<?xml version=\"1.0\" encoding=\"utf-8\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body>{body}</s:Body></s:Envelope>";
        }

        private static string CreateSoapMessageBody(SoapAction sa, UpnpArgumentList list)
        {
            var body = new StringBuilder();

            body.Append($"<u:{sa.Action} xmlns:u=\"{sa.ActionNamespace}\">");

            foreach (var property in list.Arguments)
            {
                string escapedValue = SecurityElement.Escape(property.Value.ToString());

                body.Append($"<{property.Name}>{escapedValue}</{property.Name}>");
            }

            body.Append($"</u:{sa.Action}>");

            return body.ToString();
        }

        private static StringContent CreateSoapStringContent(SoapAction soapAction, string content)
        {
            var sc = new StringContent(content);
            sc.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");
            sc.Headers.Add(SoapAction.HeaderName, soapAction.HeaderValue);
            return sc;
        }
    }
}
