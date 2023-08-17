using System.Threading;

namespace P3.Knx.Core.Console
{
   
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

           while (true)
            {
                //connection.Stop();

                //connection = new KnxConnectionTunneling(new KnxEvents(), IPAddress.Parse("192.168.8.3"), 3671,
                //    IPAddress.Parse(NetworkHelper.GetActiveIp()));
                //connection.UseNat = false;

                //connection.Start();

                Thread.Sleep(500);
            }

           

            System.Console.ReadLine();
        }
    }
}
