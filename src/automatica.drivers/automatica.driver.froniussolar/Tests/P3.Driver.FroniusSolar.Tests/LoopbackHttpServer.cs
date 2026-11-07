using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.FroniusSolar.Tests
{
    /// <summary>
    /// Minimal HTTP/1.1 server on 127.0.0.1 with an ephemeral port, one request per connection
    /// </summary>
    internal sealed class LoopbackHttpServer : IDisposable
    {
        internal sealed record Request(string Method, string Path, IReadOnlyDictionary<string, string> Headers, string Body);

        internal sealed record Response(int StatusCode, string Body, string ContentType = "application/json");

        private readonly TcpListener _listener;
        private readonly Func<Request, Response> _handler;
        private readonly CancellationTokenSource _cts = new();

        public ConcurrentQueue<Request> Requests { get; } = new();

        public int Port { get; }

        public string BaseUrl => $"http://127.0.0.1:{Port}";

        public LoopbackHttpServer(Func<Request, Response> handler)
        {
            _handler = handler;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = AcceptLoop();
        }

        private async Task AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_cts.Token);
                }
                catch
                {
                    return;
                }

                _ = Task.Run(() => Handle(client));
            }
        }

        private async Task Handle(TcpClient client)
        {
            using (client)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    var stream = client.GetStream();

                    var buffer = new List<byte>();
                    var chunk = new byte[4096];
                    var headerEnd = -1;
                    while (headerEnd < 0)
                    {
                        var read = await stream.ReadAsync(chunk, timeout.Token);
                        if (read == 0)
                        {
                            return;
                        }
                        buffer.AddRange(chunk.AsSpan(0, read).ToArray());
                        headerEnd = IndexOfHeaderEnd(buffer);
                    }

                    var headerText = Encoding.ASCII.GetString(buffer.GetRange(0, headerEnd).ToArray());
                    var lines = headerText.Split("\r\n");
                    var requestLine = lines[0].Split(' ');
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 1; i < lines.Length; i++)
                    {
                        var idx = lines[i].IndexOf(':');
                        if (idx > 0)
                        {
                            headers[lines[i].Substring(0, idx).Trim()] = lines[i].Substring(idx + 1).Trim();
                        }
                    }

                    var bodyBytes = buffer.GetRange(headerEnd + 4, buffer.Count - headerEnd - 4);
                    if (headers.TryGetValue("Content-Length", out var lengthText) && int.TryParse(lengthText, out var length))
                    {
                        while (bodyBytes.Count < length)
                        {
                            var read = await stream.ReadAsync(chunk, timeout.Token);
                            if (read == 0)
                            {
                                break;
                            }
                            bodyBytes.AddRange(chunk.AsSpan(0, read).ToArray());
                        }
                    }

                    var request = new Request(requestLine[0], requestLine.Length > 1 ? requestLine[1] : "/", headers,
                        Encoding.UTF8.GetString(bodyBytes.ToArray()));
                    Requests.Enqueue(request);

                    Response response;
                    try
                    {
                        response = _handler(request);
                    }
                    catch (Exception e)
                    {
                        response = new Response(500, e.Message, "text/plain");
                    }

                    var body = Encoding.UTF8.GetBytes(response.Body ?? string.Empty);
                    var head = $"HTTP/1.1 {response.StatusCode} {ReasonPhrase(response.StatusCode)}\r\n" +
                               $"Content-Type: {response.ContentType}; charset=utf-8\r\n" +
                               $"Content-Length: {body.Length}\r\n" +
                               "Connection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(head), timeout.Token);
                    await stream.WriteAsync(body, timeout.Token);
                    await stream.FlushAsync(timeout.Token);
                }
                catch (Exception e) when (e is IOException or OperationCanceledException or SocketException or ObjectDisposedException)
                {
                    // client went away or the test finished
                }
            }
        }

        private static int IndexOfHeaderEnd(List<byte> buffer)
        {
            for (var i = 0; i + 3 < buffer.Count; i++)
            {
                if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
                {
                    return i;
                }
            }
            return -1;
        }

        private static string ReasonPhrase(int code) => code switch
        {
            200 => "OK",
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            _ => "Status"
        };

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _cts.Dispose();
        }
    }
}
