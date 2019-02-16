using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.ZWaveAeon;
using P3.Driver.ZWaveAeon.Devices.Fibaro;
using Serilog.Core;

namespace P3.Driver.ZWave.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var controller = new ZWaveController("COM5");
            controller.Open();

            controller.Channel.Log = Console.Out;

            var setLearnMode = await controller.SetLearnMode(true);

            var result = await controller.GetNodes();

            var wallPlug = new WallPlug(result.Single(a => a.NodeID == 3));
            await wallPlug.SwitchOn();

            await Task.Delay(500);
            await wallPlug.SwitchOff();


            await Task.Delay(500);
            await wallPlug.SwitchOn();

            Console.ReadLine();
        }
    }
}
