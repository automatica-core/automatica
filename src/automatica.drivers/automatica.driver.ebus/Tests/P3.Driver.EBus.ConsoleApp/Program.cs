using System;
using System.Globalization;
using System.Threading.Tasks;

namespace P3.Driver.EBus.ConsoleApp
{
    /// <summary>
    /// Listens to a raw eBUS adapter and reads Wolf registers (read only).
    /// Usage: host port [seconds to listen] [slave address hex] [register...]
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("usage: host port [seconds] [slave hex] [register...]");
                return 1;
            }

            var listen = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 10;
            var slave = args.Length > 3 ? Convert.ToByte(args[3], 16) : (byte)0x08;

            await using var connection = new EBusConnection(new EBusTcpTransport(args[0], int.Parse(args[1], CultureInfo.InvariantCulture)), 0xFF);
            connection.TelegramReceived += t => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {t}");
            await connection.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(listen));

            for (var i = 4; i < args.Length; i++)
            {
                var register = ushort.Parse(args[i], CultureInfo.InvariantCulture);
                try
                {
                    var value = await connection.ReadWolfRegisterAsync(slave, register);
                    Console.WriteLine($"register {register}: {(value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "n/a")}");
                }
                catch (EBusException e)
                {
                    Console.WriteLine($"register {register}: {e.Message}");
                }
            }

            return 0;
        }
    }
}
