using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Logic.Surveillance.Sia
{
    /// <summary>
    /// Sends one frame to a receiver and returns its answer (null if there was none in time)
    /// </summary>
    public interface ISiaTransport
    {
        Task<string> SendAsync(string host, int port, string frame, TimeSpan timeout, CancellationToken token = default);
    }

    /// <summary>
    /// DC-09 over TCP: one connection per frame, the answer ends with CR
    /// </summary>
    public sealed class TcpSiaTransport : ISiaTransport
    {
        public static readonly TcpSiaTransport Instance = new();

        public async Task<string> SendAsync(string host, int port, string frame, TimeSpan timeout, CancellationToken token = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeout);
            try
            {
                using var client = new TcpClient { NoDelay = true };
                await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
                await using var stream = client.GetStream();

                var bytes = Encoding.ASCII.GetBytes(frame);
                await stream.WriteAsync(bytes, cts.Token).ConfigureAwait(false);

                var answer = new StringBuilder();
                var buffer = new byte[512];
                while (answer.Length < 4096)
                {
                    var read = await stream.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }
                    answer.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    if (answer.ToString().IndexOf('\r') > 0)
                    {
                        break;
                    }
                }
                return answer.Length == 0 ? null : answer.ToString();
            }
            catch (Exception e) when (e is OperationCanceledException or SocketException or IOException)
            {
                return null;
            }
        }
    }
}
