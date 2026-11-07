using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Http;

namespace P3.Driver.HomeKit.UnitTests.Integration
{
    /// <summary>
    /// Runs the real HAP connection handling (<see cref="HttpServerConnection"/> + <see cref="HapMiddleware"/>) of a
    /// <see cref="HomeKitServer"/> on 127.0.0.1 with an ephemeral port.
    /// <para>
    /// <see cref="HttpServer.Start"/> binds the dual-mode wildcard address of a fixed port and <see cref="HomeKitServer.Start"/>
    /// also starts mDNS, so this host mirrors the accept loop of <see cref="HttpServer.Start"/> instead: every accepted socket is
    /// handled by a real <see cref="HttpServerConnection"/> that is registered in the server's connection list, so notifications,
    /// <see cref="HttpServer.Stop"/> and closing removed controllers work exactly like in production.
    /// </para>
    /// </summary>
    internal sealed class LoopbackHapHost : IAsyncDisposable
    {
        public const string DeviceId = "FC:22:3D:E3:CE:F2";
        public const string PairCode = "031-45-154";

        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _acceptLoop;
        private readonly HttpServer _httpServer;
        private readonly List<HttpServerConnection> _connections;
        private readonly List<Task> _connectionTasks = new List<Task>();

        public HomeKitServer Server { get; }
        public HapMiddleware Middleware { get; }
        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public LoopbackHapHost(HomeKitServer server)
        {
            Server = server;
            var hapServer = GetField<HapControllerServer>(server, "_hapServer");
            _httpServer = GetField<HttpServer>(hapServer, "_httpServer");
            Middleware = GetField<HapMiddleware>(_httpServer, "_middleware");
            _connections = GetField<List<HttpServerConnection>>(_httpServer, "_connections");

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _acceptLoop = Task.Run(AcceptLoop);
        }

        public static HomeKitServer CreateServer(string ltsk = "", string pairings = "", string pairCode = PairCode)
        {
            return new HomeKitServer(NullLogger.Instance, 0, "test", ltsk, pairings, DeviceId, pairCode, "AutomaticaCore", "bridge", 1, "1");
        }

        public int OpenConnectionCount
        {
            get
            {
                lock (_connections)
                {
                    return _connections.Count;
                }
            }
        }

        private async Task AcceptLoop()
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return;
                }

                var connection = new HttpServerConnection(Middleware, NullLogger.Instance, client, _httpServer);
                lock (_connections)
                {
                    _connections.Add(connection);
                }

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await connection.HandleClient(token).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // HandleClient logs and closes on its own
                    }
                    finally
                    {
                        connection.Close();
                        _httpServer.ConnectionClosed(connection);
                    }
                });

                lock (_connectionTasks)
                {
                    _connectionTasks.Add(task);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();
            await _httpServer.Stop();

            Task[] tasks;
            lock (_connectionTasks)
            {
                tasks = _connectionTasks.ToArray();
            }

            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(TimeSpan.FromSeconds(5)));
            await Task.WhenAny(_acceptLoop, Task.Delay(TimeSpan.FromSeconds(5)));
            _cts.Dispose();
        }

        internal static T GetField<T>(object instance, string name)
        {
            var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(instance.GetType().Name, name);
            }

            return (T)field.GetValue(instance);
        }
    }
}
