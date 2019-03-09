using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using P3.Knx.Core.Baos.Driver;
using P3.Knx.Core.Baos.Driver.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P3.Knx.Core.Baos.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            var logger = new ConsoleLoggerProvider((s, level) => true, true).CreateLogger("test");

            var driver = new BaosDriver("/dev/ttyAMA0", logger, new DataReceiver(logger));

            await driver.Start();

            //var desc = await driver.GetDatapointDescriptions();

            logger.LogDebug($"GetDpValues...");
            var data = await driver.GetDatapointValue(1, 1);
            logger.LogDebug($"GetDpValues...done");

        
            logger.LogDebug($"setDpValue...");
            var data1 = await driver.GetDatapointValue(2, 1);
            //var sendData = await driver.SetDatapointValue(1, new byte[] { 0x00 });
            logger.LogDebug($"setDpValue...done");

            System.Console.ReadLine();

            await driver.Stop();
        }
    }

    class DataReceiver : IDatapointInd
    {
        public DataReceiver(ILogger logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; }

        public Task DatapointInd(IReadOnlyCollection<DatapointValue> values)
        {
            Logger.LogDebug($"Received datapoint values");

            return Task.CompletedTask;
        }
    }
}
