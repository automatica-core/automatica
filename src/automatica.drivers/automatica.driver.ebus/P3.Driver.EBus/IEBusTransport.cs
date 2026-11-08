using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.EBus
{
    /// <summary>
    /// The raw byte stream of the bus: every byte on the bus is received, including the ones written by us
    /// </summary>
    public interface IEBusTransport : IAsyncDisposable
    {
        Task ConnectAsync(CancellationToken token);

        /// <returns>0 if the connection was closed</returns>
        ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token);

        ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token);
    }

    /// <summary>
    /// A network adapter that bridges the bus 1:1 to a TCP port (raw mode, e.g. ser2net or esp-link). Arbitration is done by <see cref="EBusConnection"/>.
    /// </summary>
    public sealed class EBusTcpTransport : IEBusTransport
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient _client;
        private NetworkStream _stream;

        public EBusTcpTransport(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task ConnectAsync(CancellationToken token)
        {
            _client = new TcpClient { NoDelay = true };
            await _client.ConnectAsync(_host, _port, token);
            _stream = _client.GetStream();
        }

        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token) => _stream.ReadAsync(buffer, token);

        public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token) => _stream.WriteAsync(data, token);

        public ValueTask DisposeAsync()
        {
            _stream?.Dispose();
            _client?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
