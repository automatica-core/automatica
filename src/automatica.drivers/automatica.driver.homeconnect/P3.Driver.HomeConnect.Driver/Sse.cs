using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.HomeConnect.Driver
{
    internal sealed record SseEvent(string Event, string Data, string Id);

    /// <summary>
    /// Server-sent events reader (text/event-stream)
    /// </summary>
    internal static class Sse
    {
        /// <summary>
        /// Reads events until the stream ends. <paramref name="idleTimeout"/> ends the read when nothing (not even a keep-alive) arrives.
        /// </summary>
        public static async Task ReadAsync(Stream stream, Func<SseEvent, Task> onEvent, TimeSpan idleTimeout, CancellationToken token)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string eventName = null, id = null;
            var data = new StringBuilder();

            while (!token.IsCancellationRequested)
            {
                using var idle = CancellationTokenSource.CreateLinkedTokenSource(token);
                idle.CancelAfter(idleTimeout);
                string line;
                try
                {
                    line = await reader.ReadLineAsync(idle.Token);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    throw new TimeoutException($"No data on the event stream for {idleTimeout.TotalSeconds}s");
                }

                if (line == null)
                {
                    return;
                }
                if (line.Length == 0)
                {
                    if (eventName != null || data.Length > 0)
                    {
                        await onEvent(new SseEvent(eventName ?? "message", data.ToString(), id));
                    }
                    eventName = null;
                    id = null;
                    data.Clear();
                    continue;
                }
                if (line[0] == ':')
                {
                    continue; // comment / heartbeat
                }

                var colon = line.IndexOf(':');
                var field = colon < 0 ? line : line[..colon];
                var value = colon < 0 ? "" : line[(colon + 1)..];
                if (value.StartsWith(' '))
                {
                    value = value[1..];
                }
                switch (field)
                {
                    case "event":
                        eventName = value;
                        break;
                    case "data":
                        if (data.Length > 0)
                        {
                            data.Append('\n');
                        }
                        data.Append(value);
                        break;
                    case "id":
                        id = value;
                        break;
                }
            }
        }
    }
}
