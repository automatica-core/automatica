using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;

namespace Automatica.Core.Plugin.Standalone
{
    internal class ConsoleLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = formatter(state, exception);
            Console.WriteLine($"{DateTime.Now}: {msg}");   
        }
    }

    class Program
    {
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        static async Task Main(string[] args)
        {

            var logger = new ConsoleLogger();
            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("MQTT_LOG_VERBOSE")))
            {
                MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                {
                    var trace =
                        $"mqtt >> [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                    if (e.TraceMessage.Exception != null)
                    {
                        trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                    }

                    logger.LogDebug(trace);
                };
            }

            while (true)
            {
                try
                {
                    await Semaphore.WaitAsync();

                    Console.WriteLine($"Run plugin ({NetStandardUtils.Version.GetAssemblyVersion()})");

                    var execContext = new ExecutionContext(logger, args[0]);

                    if (!await execContext.Start())
                    {
                        logger.LogError("Error init connections...retry in 10sec");
                        Semaphore.Release(1);
                        await Task.Delay(10000);
                        
                        continue;
                    }

                    await execContext.Run();
                    Semaphore.Release(1);
                }
                catch (Exception e)
                {
                    Semaphore.Release(1);
                    Console.Error.WriteLine($"Error occured, retry in 10 sec\n{e}");
                    await Task.Delay(10000);
                }
            }
        }
    }
}
