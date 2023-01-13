using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using static System.Int32;

namespace Automatica.Core.NamedPipeSink
{
    public class NamedPipeSink : ILogEventSink
    {
        private readonly BlockingCollection<LogEvent> _mQueue = new BlockingCollection<LogEvent>();
        private bool _readyToEmit;

        private readonly Encoding _encoding;
        private readonly string _pipeName;
        private readonly JsonFormatter _formatter;

        internal NamedPipeSink(string pipeName, Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(pipeName)) throw new ArgumentNullException(nameof(pipeName));
            _encoding = encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            _pipeName = pipeName;
            _formatter = new JsonFormatter();

            var workerThread = new Thread(new ThreadStart(ConnectionLifetime));
            workerThread.Start();
        }

        public void Emit(LogEvent logEvent)
        {
            if (_readyToEmit) _mQueue.Add(logEvent);
        }

        private void ConnectionLifetime()
        {
            while (true)
            {
                _readyToEmit = false;
                using NamedPipeServerStream pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 254);
                pipe.WaitForConnection();

                _readyToEmit = true;
                try
                {
                    using var pipeWriter = new StreamWriter(pipe, _encoding) { AutoFlush = true };
                    while (true)
                    {
                        while (!_mQueue.IsCompleted)
                        {
                            var i = _mQueue.Take();
                            var json = SimplifiedLogEvent.Serialize(i, i.RenderMessage());
                            pipeWriter.WriteLine(json);
                        }

                        Thread.Sleep(10);
                    }
                }

                // Catch the IOException that is raised if the pipe is broken or disconnected.
                catch (IOException){ _readyToEmit = false; }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}
