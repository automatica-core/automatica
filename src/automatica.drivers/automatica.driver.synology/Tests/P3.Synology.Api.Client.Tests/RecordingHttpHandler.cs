#nullable enable
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Synology.Api.Client.Tests
{
    internal sealed class RecordedHttpRequest
    {
        public RecordedHttpRequest(HttpMethod method, Uri uri, string? contentType, string? body)
        {
            Method = method;
            Uri = uri;
            ContentType = contentType;
            Body = body;
        }

        public HttpMethod Method { get; }
        public Uri Uri { get; }
        public string? ContentType { get; }
        public string? Body { get; }
    }

    /// <summary>
    /// Fake handler that records method, url and body (read before the client disposes the content) and answers with a canned response.
    /// </summary>
    internal sealed class RecordingHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public RecordingHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public ConcurrentQueue<RecordedHttpRequest> Requests { get; } = new ConcurrentQueue<RecordedHttpRequest>();

        public static RecordingHttpHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            return new RecordingHttpHandler(_ => JsonResponse(json, status));
        }

        public static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? body = null;
            string? contentType = null;
            if (request.Content != null)
            {
                body = await request.Content.ReadAsStringAsync(cancellationToken);
                contentType = request.Content.Headers.ContentType?.MediaType;
            }

            Requests.Enqueue(new RecordedHttpRequest(request.Method, request.RequestUri!, contentType, body));
            return _respond(request);
        }

        public HttpClient ToHttpClient(string? baseAddress = "http://dsm-url.com/webapi")
        {
            var client = new HttpClient(this);
            if (baseAddress != null)
            {
                client.BaseAddress = new Uri(baseAddress);
            }
            return client;
        }
    }
}
