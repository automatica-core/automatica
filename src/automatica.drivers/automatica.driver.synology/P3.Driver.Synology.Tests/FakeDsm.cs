using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Synology.Tests
{
    internal sealed class DsmRequest
    {
        public DsmRequest(string method, string rawTarget, string contentType, string body)
        {
            Method = method;
            RawTarget = rawTarget;
            ContentType = contentType;
            Body = body;
        }

        public string Method { get; }
        public string RawTarget { get; }
        public string ContentType { get; }
        public string Body { get; }
    }

    /// <summary>
    /// In-process DSM web api (query.cgi + auth on entry.cgi) on 127.0.0.1, ephemeral port, optional https with a given certificate.
    /// </summary>
    internal sealed class FakeDsm : IAsyncDisposable
    {
        private readonly WebApplication _app;

        public string User { get; set; } = "automatica";
        public string Password { get; set; } = "made-up-dsm-password";
        public int InfoStatusCode { get; set; } = 200;

        public ConcurrentQueue<DsmRequest> Requests { get; } = new ConcurrentQueue<DsmRequest>();
        public int Port { get; private set; }

        private FakeDsm(X509Certificate2 certificate)
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory,
                EnvironmentName = "Testing"
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0, listen =>
            {
                listen.Protocols = HttpProtocols.Http1;
                if (certificate != null)
                {
                    listen.UseHttps(certificate);
                }
            }));

            _app = builder.Build();
            _app.Run(HandleAsync);
        }

        public static async Task<FakeDsm> StartAsync(X509Certificate2 certificate = null)
        {
            var dsm = new FakeDsm(certificate);
            await dsm._app.StartAsync();
            var address = dsm._app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();
            dsm.Port = new Uri(address).Port;
            return dsm;
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        private async Task HandleAsync(HttpContext context)
        {
            string body;
            using (var reader = new StreamReader(context.Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            var rawTarget = context.Features.Get<IHttpRequestFeature>().RawTarget;
            Requests.Enqueue(new DsmRequest(context.Request.Method, rawTarget, context.Request.ContentType, body));

            context.Response.ContentType = "application/json";
            var path = context.Request.Path.Value;

            if (path == "/webapi/query.cgi")
            {
                context.Response.StatusCode = InfoStatusCode;
                if (InfoStatusCode == 200)
                {
                    await context.Response.WriteAsync("{\"success\":true,\"data\":{\"SYNO.API.Auth\":{\"path\":\"entry.cgi\",\"minVersion\":1,\"maxVersion\":7}}}");
                }
                return;
            }

            if (path == "/webapi/entry.cgi" && context.Request.Method == "POST")
            {
                var form = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(body);
                var ok = form.TryGetValue("method", out var method) && method == "login"
                         && form.TryGetValue("account", out var account) && account == User
                         && form.TryGetValue("passwd", out var passwd) && passwd == Password;

                await context.Response.WriteAsync(ok
                    ? "{\"success\":true,\"data\":{\"sid\":\"made-up-sid\",\"is_portal_port\":false}}"
                    : "{\"success\":false,\"error\":{\"code\":400}}");
                return;
            }

            context.Response.StatusCode = 404;
        }
    }
}
