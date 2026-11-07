using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Camera.Tests
{
    internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string Body, string Authorization, string ContentType);

    /// <summary>
    /// Answers requests from a list of routes (first match wins) and records them
    /// </summary>
    internal sealed class FakeHttp : HttpMessageHandler
    {
        private readonly List<(Func<HttpRequestMessage, string, bool> Match, Func<HttpRequestMessage, string, HttpResponseMessage> Respond)> _routes = new();

        public List<RecordedRequest> Requests { get; } = new();

        public Exception Throw { get; set; }

        public FakeHttp On(HttpMethod method, string pathAndQueryStart, Func<HttpRequestMessage, string, HttpResponseMessage> respond)
        {
            _routes.Add(((r, _) => r.Method == method && r.RequestUri!.PathAndQuery.StartsWith(pathAndQueryStart, StringComparison.Ordinal), respond));
            return this;
        }

        public FakeHttp OnSoap(string path, string operation, string responseBody, HttpStatusCode status = HttpStatusCode.OK)
        {
            _routes.Add(((r, body) => r.Method == HttpMethod.Post && r.RequestUri!.AbsolutePath == path &&
                                      (body.Contains($"<{operation} ") || body.Contains($"<{operation}>") || body.Contains($":{operation} ") || body.Contains($":{operation}>")),
                (_, _) => Soap(responseBody, status)));
            return this;
        }

        public HttpClient Client() => new(this);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            lock (Requests)
            {
                Requests.Add(new RecordedRequest(request.Method, request.RequestUri, body, request.Headers.Authorization?.ToString(),
                    request.Content?.Headers.ContentType?.ToString()));
            }

            if (Throw != null)
            {
                throw Throw;
            }

            foreach (var (match, respond) in _routes)
            {
                if (match(request, body ?? ""))
                {
                    return respond(request, body);
                }
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("not found") };
        }

        public static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        public static HttpResponseMessage Ok() => new(HttpStatusCode.OK) { Content = new StringContent("") };

        public static HttpResponseMessage Soap(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
        {
            Content = new StringContent(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://www.w3.org/2003/05/soap-envelope\" " +
                "xmlns:tt=\"http://www.onvif.org/ver10/schema\" xmlns:tds=\"http://www.onvif.org/ver10/device/wsdl\" xmlns:trt=\"http://www.onvif.org/ver10/media/wsdl\" " +
                "xmlns:tev=\"http://www.onvif.org/ver10/events/wsdl\" xmlns:wsnt=\"http://docs.oasis-open.org/wsn/b-2\" xmlns:wsa5=\"http://www.w3.org/2005/08/addressing\" " +
                "xmlns:tns1=\"http://www.onvif.org/ver10/topics\"><SOAP-ENV:Body>" + body + "</SOAP-ENV:Body></SOAP-ENV:Envelope>",
                Encoding.UTF8, "application/soap+xml")
        };

        public IReadOnlyList<RecordedRequest> To(string pathStart) => Requests.Where(r => r.Uri.PathAndQuery.StartsWith(pathStart, StringComparison.Ordinal)).ToList();
    }
}
