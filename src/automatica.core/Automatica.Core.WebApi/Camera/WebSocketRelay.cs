using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.WebApi.Camera
{
    /// <summary>
    /// Copies the messages of two WebSockets in both directions (keeping text / binary and message boundaries)
    /// until one side closes, the token is cancelled, or an error ends one direction; then both are closed.
    /// </summary>
    public static class WebSocketRelay
    {
        private const int BufferSize = 64 * 1024;

        public static async Task RunAsync(WebSocket client, WebSocket upstream, CancellationToken token)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            var toUpstream = PumpAsync(client, upstream, cts.Token);
            var toClient = PumpAsync(upstream, client, cts.Token);

            await Task.WhenAny(toUpstream, toClient);
            await cts.CancelAsync();

            await CloseAsync(client);
            await CloseAsync(upstream);

            try
            {
                await Task.WhenAll(toUpstream, toClient);
            }
            catch
            {
                // the pump that did not end the relay was cancelled
            }
        }

        private static async Task PumpAsync(WebSocket source, WebSocket destination, CancellationToken token)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }
                    if (destination.State != WebSocketState.Open)
                    {
                        return;
                    }
                    await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, token);
                }
            }
            catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or ObjectDisposedException)
            {
                // the connection is gone, RunAsync closes the other side
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task CloseAsync(WebSocket socket)
        {
            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token);
                }
            }
            catch (Exception)
            {
                socket.Abort();
            }
        }
    }
}
