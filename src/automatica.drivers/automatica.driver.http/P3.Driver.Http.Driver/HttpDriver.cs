using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.Http.Driver.Security;

namespace P3.Driver.Http.Driver
{
    /// <summary>
    /// Root of the virtual HTTP inputs and outputs, owns the HTTP client that every device uses
    /// </summary>
    internal sealed class HttpDriver : DriverNoneAttributeBase
    {
        private readonly object _clientLock = new();
        private HttpClient _client;

        public HttpDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        /// <summary>
        /// Requests to the core's own host (localhost, 127.0.0.1, ::1) are blocked unless this is on
        /// </summary>
        internal bool AllowLocalHost => GetPropertyValueBool(HttpDriverFactory.AllowLocalHostKey) ?? false;

        /// <summary>
        /// Creates the handler of the HTTP client, tests replace it with a fake
        /// </summary>
        internal Func<HttpMessageHandler> HandlerFactory { get; set; }

        private HttpClient Client
        {
            get
            {
                lock (_clientLock)
                {
                    if (_client == null)
                    {
                        var handler = HandlerFactory?.Invoke() ?? RequestGuard.CreateHandler(() => AllowLocalHost);
                        _client = new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
                    }
                    return _client;
                }
            }
        }

        /// <summary>
        /// Sends one request. Never throws for network errors, timeouts and blocked addresses, the result carries the reason.
        /// </summary>
        internal async Task<HttpResult> SendAsync(HttpMethod method, Uri uri, string body, string contentType, Credentials credentials,
            TimeSpan timeout, bool readBody, CancellationToken token = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeout);

            using var request = new HttpRequestMessage(method, uri);
            request.Headers.UserAgent.ParseAdd("Automatica-Http/1.0");
            if (credentials.IsSet)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials.BasicValue());
            }
            if (body != null && method != HttpMethod.Get)
            {
                request.Content = new StringContent(body, Encoding.UTF8);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var type)
                    ? type
                    : new MediaTypeHeaderValue(HttpDriverFactory.DefaultContentType) { CharSet = "utf-8" };
            }

            try
            {
                using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
                var status = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    var reason = status >= 300 && status < 400 ? $"HTTP {status} (redirects are not followed)" : $"HTTP {status}";
                    return new HttpResult(status, null, reason);
                }
                var text = readBody ? await RequestGuard.ReadBodyAsync(response, cts.Token).ConfigureAwait(false) : null;
                return new HttpResult(status, text, null);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return new HttpResult(0, null, $"no answer within {timeout.TotalSeconds:0.#}s");
            }
            catch (RequestBlockedException ex)
            {
                return new HttpResult(0, null, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                var blocked = Inner<RequestBlockedException>(ex);
                return new HttpResult(0, null, blocked?.Message ?? ex.Message);
            }
        }

        private static T Inner<T>(Exception ex) where T : Exception
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is T match)
                {
                    return match;
                }
            }
            return null;
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            lock (_clientLock)
            {
                _client?.Dispose();
                _client = null;
            }
            return base.Stop(token);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            switch (ctx.NodeInstance.This2NodeTemplateNavigation?.Key)
            {
                case "input":
                    return new InputDeviceNode(ctx, this);
                case "output":
                    return new OutputDeviceNode(ctx, this);
            }
            return null;
        }
    }

    internal readonly record struct HttpResult(int Status, string Body, string Error)
    {
        public bool Success => Error == null;
    }

    internal readonly record struct Credentials(string User, string Password)
    {
        public bool IsSet => !string.IsNullOrEmpty(User);

        public string BasicValue() => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{User}:{Password ?? ""}"));

        /// <summary>
        /// Keeps the password out of logs and exception messages
        /// </summary>
        public override string ToString() => IsSet ? $"{User}:***" : "";
    }

    internal static class HttpLog
    {
        /// <summary>
        /// Warning text when credentials would go over plain http
        /// </summary>
        public static string PlainHttpWarning(string name, Uri uri) =>
            $"{name}: the user and password are sent unencrypted to {RequestGuard.ForLog(uri)}, use https if the device supports it";

        public static bool IsPlainHttp(Uri uri) => uri != null && uri.Scheme == Uri.UriSchemeHttp;

        public static string Trim(string text, int max = 200) =>
            text == null ? "" : text.Length <= max ? text : text.Substring(0, max) + "…";
    }
}
