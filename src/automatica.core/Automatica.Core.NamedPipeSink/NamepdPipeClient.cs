using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Automatica.Core.NamedPipeSink
{
    public class NamedPipeClient
    {
        private NamedPipeClientStream _pipeMessages;
        private readonly string _pipeName;
        private Thread _workerThread;
        private readonly Serilog.ILogger _logger;

        public NamedPipeClient(string pipeName, ILogger logger)
        {
            _pipeName = pipeName;
            _workerThread = new Thread(LifetimeConnection);
            _workerThread.Start();
            _logger = logger;
        }

        private void LifetimeConnection()
        {
            while (true)
            {
                try
                {
                    using (_pipeMessages = new NamedPipeClientStream(".", _pipeName, PipeDirection.In))
                    {
                        ConnectToApp();
                        ReadPipeForever();
                    }

                    Console.WriteLine("Disconnected from: "+ _pipeName);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void ConnectToApp()
        {
            Console.Write("/rAttempting to connect to {0}", _pipeName);
            int k = 0;
            while (true)
            {
                if (DoesNamedPipeExist(_pipeName))
                {
                    try
                    {
                        _pipeMessages.Connect(500);
                        break;
                    }
                    catch (TimeoutException) { }
                }
                switch (k++ % 5)
                {
                    case 1: Console.Write("\rAttempting to connect to '" + _pipeName + "' /"); break;
                    case 2: Console.Write("\rAttempting to connect to '" + _pipeName + "' -"); break;
                    case 3: Console.Write("\rAttempting to connect to '" + _pipeName + "' |"); break;
                    case 4: Console.Write("\rAttempting to connect to '" + _pipeName + "' \\"); break;
                }

                Thread.Sleep(500);
            }

            Console.Clear();
            _logger.Debug("Connected");
        }

        private void ReadPipeForever()
        {
            using var srPipe = new StreamReader(_pipeMessages);
            while (_pipeMessages.IsConnected)
            {
                var txt = srPipe.ReadLine();
                if (txt != null)
                {
                    var logEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<SimplifiedLogEvent>(txt);
                    _logger.Write(logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, logEvent.Properties);
                }
                else Thread.Sleep(10);
            }
        }

        private bool DoesNamedPipeExist(string pipeName)
        {
            try
            {
                return WaitNamedPipe(@"\\.\pipe\" + pipeName, 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WaitNamedPipe(string name, int timeout);
    }

    public class ClientEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Properties.Values.FirstOrDefault() is DictionaryValue prop)
            {
                foreach (var p in prop.Elements)
                {
                    var key = p.Key.Value.ToString();
                    var value = p.Value;
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(key, value));
                }
            }
        }
    }
}
