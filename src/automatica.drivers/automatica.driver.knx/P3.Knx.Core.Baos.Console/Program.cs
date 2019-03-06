using Microsoft.Extensions.Logging.Console;
using P3.Knx.Core.Baos.Driver;
using System.Threading.Tasks;

namespace P3.Knx.Core.Baos.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            var logger = new ConsoleLoggerProvider((s, level) => true, true).CreateLogger("test");

            var driver = new BaosDriver("/dev/ttyAMA0", logger);

            await driver.Start();

            System.Console.ReadLine();

            await driver.Stop();
        }
    }
}
