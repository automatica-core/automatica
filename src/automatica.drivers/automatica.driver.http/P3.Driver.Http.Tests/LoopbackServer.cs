using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Http.Tests
{
    /// <summary>
    /// A tiny HTTP server on 127.0.0.1 that answers every connection with a fixed response
    /// </summary>
    internal sealed class LoopbackServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly byte[] _response;
        private readonly CancellationTokenSource _cts = new();
        private Task _loop;
        private int _requests;

        private LoopbackServer(string response)
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _response = Encoding.ASCII.GetBytes(response);
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public string Url => $"http://127.0.0.1:{Port}/";

        public int Requests => Volatile.Read(ref _requests);

        public static Task<LoopbackServer> StartAsync(string response)
        {
            var server = new LoopbackServer(response);
            server._listener.Start();
            server._loop = Task.Run(server.AcceptLoop);
            return Task.FromResult(server);
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
                catch (Exception)
                {
                    return;
                }

                using (client)
                {
                    try
                    {
                        var stream = client.GetStream();
                        var buffer = new byte[8192];
                        var request = new StringBuilder();
                        while (!request.ToString().Contains("\r\n\r\n"))
                        {
                            var read = await stream.ReadAsync(buffer, _cts.Token);
                            if (read == 0)
                            {
                                break;
                            }
                            request.Append(Encoding.ASCII.GetString(buffer, 0, read));
                        }
                        if (request.Length > 0)
                        {
                            Interlocked.Increment(ref _requests);
                            LastRequest = request.ToString();
                            await stream.WriteAsync(_response, _cts.Token);
                        }
                    }
                    catch (Exception)
                    {
                        // client went away
                    }
                }
            }
        }

        public string LastRequest { get; private set; }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();
            try
            {
                await _loop;
            }
            catch (Exception)
            {
                // stopped
            }
            _cts.Dispose();
        }
    }
}
